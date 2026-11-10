"""Build and verify FocusCapture installation ZIPs. No runtime installation side effects."""
from hashlib import sha256
from io import BytesIO
from pathlib import Path, PurePosixPath
from zipfile import ZipFile, ZipInfo, ZIP_DEFLATED
import argparse
import fnmatch
import json
import re

ROOT = Path(__file__).resolve().parent.parent
MAX_ICON_BYTES = 256 * 1024
TEXT_SUFFIXES = {'.js', '.mjs', '.json', '.cs', '.py', '.md', '.yml', '.yaml', '.txt'}
USER_PATH = re.compile(r'(?i)[a-z]:[\\/]+users[\\/]+[^\\/\s]+')
REQUIRED = {
    'host-v1.1.js', 'index.js', 'runtime.js', 'hotkey.js', 'settings.js', 'client.js',
    'cordis.patch.yml', 'native/DualCtrlSnip.exe', 'native/keyboard-worker.py',
    'native/keyboard-runtime.json', 'native/Typography.cs', 'native/fonts/OFL.txt', 'icon.png',
}


def manifest_files(root):
    manifest = json.loads((root / 'package.json').read_text(encoding='utf-8'))
    files = {'package.json'}
    for pattern in manifest['files']:
        matched = [p for p in root.glob(pattern) if p.is_file()]
        if not matched:
            raise ValueError(f'Manifest pattern has no files: {pattern}')
        files.update(p.relative_to(root).as_posix() for p in matched)
    return manifest, sorted(files)


def icon_check(data):
    if len(data) >= MAX_ICON_BYTES:
        raise ValueError(f'Icon exceeds 256 KiB: {len(data)} bytes')
    try:
        from PIL import Image
        with Image.open(BytesIO(data)) as image:
            if image.format != 'PNG':
                raise ValueError('Icon is not PNG')
            image.verify()
        with Image.open(BytesIO(data)) as image:
            width, height = image.size
            if width < 64 or height < 64 or width != height:
                raise ValueError(f'Icon must be square and at least 64px: {width}x{height}')
            return width, height
    except ImportError:
        raise ValueError('Pillow is required for release verification: pip install -r requirements-dev.txt') from None


def verify_archive(path):
    path = Path(path).resolve()
    with ZipFile(path) as archive:
        bad = archive.testzip()
        if bad:
            raise ValueError(f'ZIP CRC failure: {bad}')
        names = archive.namelist()
        roots = {PurePosixPath(name).parts[0] for name in names}
        if roots != {'dsh-FocusCapture'}:
            raise ValueError(f'Unexpected archive roots: {roots}')
        for name in names:
            pure = PurePosixPath(name)
            if pure.is_absolute() or '..' in pure.parts:
                raise ValueError(f'Unsafe archive path: {name}')
        members = {PurePosixPath(name).relative_to('dsh-FocusCapture').as_posix(): name for name in names}
        missing = REQUIRED - members.keys()
        if missing:
            raise ValueError(f'Missing installation files: {sorted(missing)}')
        manifest = json.loads(archive.read(members['package.json']).decode('utf-8'))
        if manifest.get('name') != '@local/dsh-focuscapture' or not re.fullmatch(r'\d+\.\d+\.\d+', manifest.get('version', '')):
            raise ValueError('Invalid package identity or version')
        for key in ('./icon', './host-v1.1/icon'):
            target = manifest['exports'][key].removeprefix('./')
            if target not in members:
                raise ValueError(f'Missing exported icon: {target}')
        icon = archive.read(members[manifest['icon']])
        width, height = icon_check(icon)
        config = json.loads(archive.read(members['native/keyboard-runtime.json']).decode('utf-8'))
        if config.get('pythonExecutable') not in (None, ''):
            raise ValueError('Release must not contain a default personal Python path')
        for relative, name in members.items():
            if PurePosixPath(relative).suffix.lower() in TEXT_SUFFIXES:
                text = archive.read(name).decode('utf-8')
                match = USER_PATH.search(text)
                if match:
                    raise ValueError(f'Personal absolute path in {relative}: {match.group(0)}')
        if b'/* INSERT_' in archive.read(members['client.js']):
            raise ValueError('Client contains an unbuilt insertion placeholder')
        return {
            'archive': str(path), 'version': manifest['version'], 'files': len(names),
            'bytes': path.stat().st_size, 'sha256': sha256(path.read_bytes()).hexdigest(),
            'iconBytes': len(icon), 'iconSize': [width, height],
            'runtimeConfig': 'portable', 'personalPaths': 0, 'crc': True,
        }


def build():
    manifest, files = manifest_files(ROOT)
    version = manifest['version']
    dist = ROOT / 'dist'
    dist.mkdir(exist_ok=True)
    destination = dist / f'dsh-FocusCapture-{version}-install.zip'
    with ZipFile(destination, 'w', compression=ZIP_DEFLATED, compresslevel=9) as archive:
        for relative in files:
            info = ZipInfo(f'dsh-FocusCapture/{relative}', date_time=(1980, 1, 1, 0, 0, 0))
            info.compress_type = ZIP_DEFLATED
            info.external_attr = 0o100644 << 16
            archive.writestr(info, (ROOT / relative).read_bytes(), compress_type=ZIP_DEFLATED, compresslevel=9)
    result = verify_archive(destination)
    checksums = dist / 'SHA256SUMS.txt'
    checksums.write_text(f"{result['sha256']}  {destination.name}\n", encoding='utf-8', newline='\n')
    result['checksums'] = str(checksums)
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest='command', required=True)
    sub.add_parser('build')
    verify = sub.add_parser('verify')
    verify.add_argument('archive', type=Path)
    args = parser.parse_args()
    result = build() if args.command == 'build' else verify_archive(args.archive)
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == '__main__':
    main()
