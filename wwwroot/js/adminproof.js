/* ============================================================
   CateringFlow - Admin Payment Proofs / Chat JS
   ============================================================ */

document.addEventListener('DOMContentLoaded', () => {
  const chatModal = document.getElementById('proofChatModal');
  if (!chatModal) return;

  let activeProofId = 0;

  window.openAdminProofChat = function (proofId) {
    activeProofId = proofId;
    chatModal.classList.add('open');
    loadProofChat(proofId);
  };

  window.closeAdminProofChat = function () {
    chatModal.classList.remove('open');
  };

  chatModal.addEventListener('click', (e) => {
    if (e.target.id === 'proofChatModal') {
      window.closeAdminProofChat();
    }
  });

  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') window.closeAdminProofChat();
  });

  // Ignore the admin-modal-backdrop CSS' display rules; view-specific styling.
  chatModal.style.display = 'none';
  chatModal.classList.remove('open');

  async function loadProofChat(proofId) {
    const list = document.getElementById('proofChatMessages');
    if (!list) return;
    list.innerHTML = '<div class="text-center text-muted py-4"><div class="spinner-border spinner-border-sm"></div></div>';

    try {
      const resp = await fetch('/SuperAdmin/ProofAdminChat?id=' + proofId);
      if (!resp.ok) return;
      const data = await resp.json();
      const proof = data.proof || {};

      document.getElementById('proofChatTitle').textContent = 'Payment Chat · ' + (proof.eventName || '');
      document.getElementById('proofChatMeta').textContent =
        (proof.customerName || '') + ' · ' + (proof.paymentMethod || '') +
        (proof.referenceNumber ? ' · Ref #' + proof.referenceNumber : '') +
        ' · ₱' + Number(proof.amount || 0).toLocaleString(undefined, { minimumFractionDigits: 2 }) +
        ' · ' + (proof.status || '');

      list.innerHTML = '';
      const messages = data.messages || [];
      if (!messages.length) {
        const empty = document.createElement('div');
        empty.className = 'text-center text-muted small py-4';
        empty.textContent = 'No messages yet.';
        list.appendChild(empty);
      }
      messages.forEach(m => {
        const isAdmin = m.senderRole === 'Admin';
        const div = document.createElement('div');
        div.className = 'proof-chat-msg ' + (isAdmin ? 'proof-msg-admin' : 'proof-msg-customer');
        div.innerHTML = '<div class="proof-chat-name">' + escapeHtml(m.senderName) + '</div>'
          + '<div class="proof-chat-text">' + escapeHtml(m.message) + '</div>'
          + '<div class="proof-chat-time">' + m.createdAt + '</div>';
        list.appendChild(div);
      });

      if (proof.proofImagePath) {
        const imgDiv = document.createElement('div');
        imgDiv.className = 'text-center mt-2';
        imgDiv.innerHTML = '<a href="' + proof.proofImagePath + '" target="_blank"><img src="' + proof.proofImagePath
          + '" alt="Proof" style="max-height:140px;border-radius:10px;border:1px solid #e8e2d8;" /></a>';
        list.appendChild(imgDiv);
      }

      list.scrollTop = list.scrollHeight;
    } catch (_) { /* ignore */ }
  }

  const sendBtn = document.getElementById('proofChatSendBtn');
  if (sendBtn) {
    sendBtn.addEventListener('click', async () => {
      const input = document.getElementById('proofChatInput');
      const message = input?.value?.trim();
      if (!message || !activeProofId) return;

      const csrf = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
      sendBtn.disabled = true;
      try {
        const form = new FormData();
        form.append('proofId', activeProofId);
        form.append('message', message);
        const resp = await fetch('/SuperAdmin/SendProofAdminMessage', {
          method: 'POST',
          headers: { 'RequestVerificationToken': csrf },
          body: form
        });
        if (resp.ok) {
          input.value = '';
          await loadProofChat(activeProofId);
        }
      } finally {
        sendBtn.disabled = false;
      }
    });
  }
});

function escapeHtml(str) {
  const div = document.createElement('div');
  div.textContent = str;
  return div.innerHTML;
}