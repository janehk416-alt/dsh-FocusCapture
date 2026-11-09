import { randomUUID } from 'node:crypto';

export const LEASE_MS = 180_000;
export const IMAGE_TTL_MS = 300_000;
export const MAX_IMAGE_BYTES = 20 * 1024 * 1024;
export function pngInfo(data) {
  const bytes = Buffer.from(data, 'base64');
  if (bytes.length < 33 || bytes.length > MAX_IMAGE_BYTES || !bytes.subarray(0, 8).equals(Buffer.from([137,80,78,71,13,10,26,10])) || bytes.toString('ascii',12,16) !== 'IHDR') throw new Error('Invalid or oversized screenshot PNG');
  const width = bytes.readUInt32BE(16), height = bytes.readUInt32BE(20);
  if (!width || !height || width * height > 25_000_000) throw new Error('Screenshot dimensions exceed the limit');
  return { bytes, width, height };
}
export class CaptureState {
  constructor(now = Date.now) {
    this.now = now; this.owner = null; this.capture = null; this.images = [];
    this.ready = false; this.lastError = null; this.lastDelivery = null;
  }
  expire() {
    const now = this.now();
    if (this.owner && now - this.owner.seen > LEASE_MS) this.owner = null;
    this.images = this.images.filter(x => now - x.created < IMAGE_TTL_MS);
    if (this.capture && now - this.capture.started > 130_000) { this.capture = null; this.lastError = 'Capture timed out'; }
  }
  bind({ clientId, sessionId, focused, locale }) {
    this.expire();
    if (!this.owner || this.owner.clientId === clientId || focused) {
      this.owner = { clientId, sessionId, locale: locale === 'en' ? 'en' : 'zh', seen: this.now() };
    }
    return this.owner?.clientId === clientId;
  }
  detach(clientId) { if (this.owner?.clientId === clientId) this.owner = null; }
  begin(clientId) {
    this.expire();
    if (!this.ready) throw new Error('Native screenshot helper is not ready');
    if (!this.owner || (clientId && this.owner.clientId !== clientId)) throw new Error('Open and activate a DSH chat before capturing');
    if (this.capture) throw new Error('A screenshot is already in progress');
    if (this.images.length >= 3) throw new Error('Finish pending screenshots before taking another');
    // Snapshot the receiving Session NOW. Switching chats during selection never reroutes the image.
    this.capture = { ...this.owner, started: this.now() };
    this.lastError = null;
    return this.capture;
  }
  finish(data) {
    const target = this.capture; this.capture = null;
    if (!target) throw new Error('Screenshot arrived without a bound capture');
    const { bytes, width, height } = pngInfo(data);
    const item = { id: randomUUID(), clientId: target.clientId, sessionId: target.sessionId, created: this.now(), width, height, bytes, name: `screenshot-${new Date(this.now()).toISOString().replace(/[:.]/g,'-')}.png` };
    this.images.push(item);
    return item;
  }
  poll(clientId, sessionId) {
    this.expire();
    return this.images.filter(x => x.clientId === clientId && x.sessionId === sessionId);
  }
  ack(clientId, id) {
    const found = this.images.find(x => x.id === id && x.clientId === clientId);
    if (!found) return false;
    this.images = this.images.filter(x => x !== found);
    this.lastDelivery = { sessionId: found.sessionId, at: this.now(), width: found.width, height: found.height };
    return true;
  }
  status() {
    this.expire();
    return { ready: this.ready, armed: !!this.owner && this.ready, hotkey: 'LeftCtrl+RightCtrl', targetSession: this.owner?.sessionId ?? null, capturing: !!this.capture, pending: this.images.length, lastError: this.lastError, lastDelivery: this.lastDelivery };
  }
}
