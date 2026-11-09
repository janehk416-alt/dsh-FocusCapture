// Shared pure browser-file admission operation; used by the built Client and unit tests.
export function attachScreenshot(conversation, input, sessionId, image) {
  if (!conversation?.createDrafts || !conversation?.releaseDraftAttachments || !input?.addAttachments || !sessionId) throw new Error('Image attachment intake is unavailable');
  const binary = atob(image.data);
  const bytes = Uint8Array.from(binary, char => char.charCodeAt(0));
  const file = new File([bytes], image.name, { type:'image/png' });
  const created = conversation.createDrafts(sessionId,[file],false);
  try {
    if (!created.length || input.addAttachments(created.map(item => item.id)) !== true) throw new Error('The composer is busy; screenshot is kept for retry');
  } catch(error) { conversation.releaseDraftAttachments(created); throw error; }
  return created;
}
