/* ============================================================
   CateringFlow - Client JS
   ============================================================ */

let currentWizardStep = 1;
let selectedPackageName = null;

document.addEventListener('DOMContentLoaded', () => {
  // Sticky / Scrolled Navbar behavior
  const navbar = document.querySelector('.client-navbar');
  if (navbar) {
    const handleScroll = () => {
      if (window.scrollY > 30) {
        navbar.classList.add('scrolled');
      } else {
        navbar.classList.remove('scrolled');
      }
    };
    window.addEventListener('scroll', handleScroll, { passive: true });
    handleScroll();
  }

  // Mobile navigation toggle
  window.toggleMobileNav = function () {
    const navLinks = document.getElementById('clientNavLinks');
    if (navLinks) {
      navLinks.classList.toggle('mobile-open');
    }
  };

  // Close mobile nav when clicking on a link
  document.querySelectorAll('#clientNavLinks a').forEach(link => {
    link.addEventListener('click', () => {
      const navLinks = document.getElementById('clientNavLinks');
      if (navLinks) navLinks.classList.remove('mobile-open');
    });
  });

  // Sign-in prompt modal (shown when a signed-out visitor tries to book)
  window.closeSignInPrompt = function () {
    const modal = document.getElementById('signInPromptModal');
    if (modal) modal.classList.remove('open');
  };

  document.getElementById('signInPromptModal')?.addEventListener('click', (e) => {
    if (e.target.id === 'signInPromptModal') {
      window.closeSignInPrompt();
    }
  });

  initBookingWizard();
  initPackageDetails();
  initPaymentChat();
  initInquiryForm();

  // Close booking modal when clicking / pressing Escape
  document.getElementById('bookingModal')?.addEventListener('click', (e) => {
    if (e.target.id === 'bookingModal') {
      window.closeBookingModal();
    }
  });
  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
      window.closeBookingModal();
      window.closePackageDetails();
    }
  });
});

/* ============================================================
   Package Details Modal
   ============================================================ */
function initPackageDetails() {
  window.openPackageDetails = function (index) {
    const packages = window.__CATERINGFLOW_PACKAGES__ || [];
    const pkg = packages[index];
    if (!pkg) return;

    window.selectedPackageIndex = index;
    selectedPackageName = pkg.packageName;

    document.getElementById('pkgDetailTitle').textContent = pkg.packageName;
    document.getElementById('pkgDetailName').textContent = pkg.packageName;
    document.getElementById('pkgDetailDesc').textContent = pkg.description;
    document.getElementById('pkgDetailImg').src = '/images/' + pkg.image;
    document.getElementById('pkgDetailPrice').textContent = '₱' + Number(pkg.price).toLocaleString();
    document.getElementById('pkgDetailCourses').textContent = pkg.courseCount + (pkg.courseCount > 1 ? ' Courses' : ' Course');
    document.getElementById('pkgDetailHours').textContent = pkg.serviceHours + ' Hours Service';

    const highlightChip = document.getElementById('pkgDetailHighlightChip');
    highlightChip.textContent = pkg.highlight || 'Curated Menu';

    const hl = document.getElementById('pkgDetailHighlight');
    if (pkg.highlight) {
      hl.style.display = '';
      hl.textContent = pkg.highlight;
    } else {
      hl.style.display = 'none';
    }

    const list = document.getElementById('pkgDetailFeatures');
    list.innerHTML = '';
    (pkg.features || []).forEach(f => {
      const li = document.createElement('li');
      li.innerHTML = '<i class="bi bi-check2-circle"></i> ' + f;
      list.appendChild(li);
    });

    document.getElementById('packageDetailsModal').classList.add('open');
  };

  window.closePackageDetails = function () {
    const modal = document.getElementById('packageDetailsModal');
    if (modal) modal.classList.remove('open');
  };

  window.bookFromPackageDetails = function () {
    closePackageDetails();
    openBookingModal(selectedPackageName);
  };

  document.getElementById('packageDetailsModal')?.addEventListener('click', (e) => {
    if (e.target.id === 'packageDetailsModal') {
      window.closePackageDetails();
    }
  });
}

/* ============================================================
   Booking Wizard
   ============================================================ */
function initBookingWizard() {
  window.openBookingModal = function (packageName = '') {
    const isAuthenticated = window.__CATERINGFLOW_AUTH__ === 'true';

    // Signed-out visitors must sign in before booking.
    if (!isAuthenticated) {
      const prompt = document.getElementById('signInPromptModal');
      if (prompt) prompt.classList.add('open');
      return;
    }

    selectedPackageName = packageName || selectedPackageName;

    resetWizard();
    prefillUserDetails();
    if (selectedPackageName) {
      const pkgSelect = document.getElementById('bookingPackageId');
      if (pkgSelect) {
        const options = Array.from(pkgSelect.options);
        const match = options.find(o => o.text.startsWith(selectedPackageName));
        if (match) pkgSelect.value = match.value;
      }
    }
    updateAmountSummary();

    document.getElementById('bookingModal')?.classList.add('open');
    wizardGoTo(1);
  };

  window.closeBookingModal = function () {
    const modal = document.getElementById('bookingModal');
    if (modal) modal.classList.remove('open');
  };

  window.wizardNext = function () {
    if (currentWizardStep === 1 && !validateStep1()) return;
    if (currentWizardStep === 2 && !validateStep2()) return;
    if (currentWizardStep === 3 && !validateStep3()) return;
    if (currentWizardStep >= 4) return;
    wizardGoTo(currentWizardStep + 1);
  };

  window.wizardGoTo = function (step) {
    if (step < 1) step = 1;
    if (step > 4) step = 4;
    currentWizardStep = step;

    document.querySelectorAll('.booking-wizard-panel').forEach(p => {
      p.classList.toggle('d-none', parseInt(p.dataset.wzPanel, 10) !== step);
    });

    document.querySelectorAll('.wizard-step').forEach(dot => {
      const st = parseInt(dot.dataset.wzStep, 10);
      dot.classList.toggle('active', st === step);
      dot.classList.toggle('done', st < step);
    });

    document.querySelector('.booking-wizard-step-label').textContent = `Step ${step} of 4 — `
      + (step === 1 ? 'Your Details' : step === 2 ? 'Choose Payment Method' : step === 3 ? 'Proof of Payment' : 'Review & Confirmation');

    const backBtn = document.getElementById('wizardBackBtn');
    const nextBtn = document.getElementById('wizardNextBtn');
    const submitBtn = document.getElementById('wizardSubmitBtn');

    backBtn.style.display = step === 1 ? 'none' : '';
    nextBtn.style.display = step === 4 ? 'none' : '';
    submitBtn.style.display = step === 4 ? '' : 'none';

    if (step === 2) updateAmountSummary();
    if (step === 4) populateReview();
  };

  function resetWizard() {
    const form = document.getElementById('clientBookingForm');
    if (form) form.reset();
    document.getElementById('bookingEmail')?.setAttribute('readonly', 'readonly');
    document.querySelectorAll('.pm-card').forEach(c => c.classList.remove('selected'));
    document.querySelectorAll('.pm-qr-panel').forEach(p => p.classList.add('d-none'));
    document.getElementById('proofPreview')?.classList.add('d-none');
    document.getElementById('proofPreview')?.removeAttribute('src');

    const pkgSelect = document.getElementById('bookingPackageId');
    if (pkgSelect) {
      const options = Array.from(pkgSelect.options);
      const preferred = options.find(o => o.text.startsWith('Premium')) || options[1];
      if (preferred) pkgSelect.value = preferred.value;
    }
  }

  function validateStep1() {
    const name = document.getElementById('bookingFullName')?.value.trim();
    const phone = document.getElementById('bookingPhone')?.value.trim();
    const date = document.getElementById('bookingDate')?.value;
    const pax = parseInt(document.getElementById('bookingPax')?.value || '0', 10);

    if (!name) { alert('Please enter your full name.'); return false; }
    if (!phone) { alert('Please enter your phone number.'); return false; }
    if (!date) { alert('Please select an event date.'); return false; }
    if (!pax || pax <= 0) { alert('Please enter a valid guest count.'); return false; }
    return true;
  }

  function validateStep2() {
    const method = document.querySelector('input[name="paymentMethod"]:checked');
    if (!method) {
      alert('Please choose a payment method (GCash, PayMaya, or Credit Card).');
      return false;
    }
    return true;
  }

  function validateStep3() {
    const proof = document.getElementById('bookingProofImage');
    if (!proof || !proof.files.length) {
      alert('Please upload a screenshot / proof of your payment.');
      return false;
    }
    return true;
  }

  // Pre-fill the name / email / phone from the signed-in user.
  async function prefillUserDetails() {
    try {
      const resp = await fetch('/Client/CurrentUser');
      if (!resp.ok) return;
      const data = await resp.json();
      const nameField = document.getElementById('bookingFullName');
      const phoneField = document.getElementById('bookingPhone');
      const emailField = document.getElementById('bookingEmail');
      if (nameField && !nameField.value) nameField.value = data.name || '';
      if (phoneField && !phoneField.value) phoneField.value = data.phone || '';
      if (emailField) emailField.value = data.email || '';
    } catch (_) { /* ignore */ }
  }

  function getSelectedPackagePrice() {
    const pkgSelect = document.getElementById('bookingPackageId');
    const opt = pkgSelect?.selectedOptions?.[0];
    return opt ? parseFloat(opt.dataset.price || '0') : 0;
  }

  function updateAmountSummary() {
    const pax = parseInt(document.getElementById('bookingPax')?.value || '0', 10);
    const price = getSelectedPackagePrice();
    const total = pax * price;

    const pkgSelect = document.getElementById('bookingPackageId');
    const summaryPackage = document.getElementById('summaryPackage');
    const summaryPax = document.getElementById('summaryPax');
    const summaryTotal = document.getElementById('summaryTotal');

    if (summaryPackage) summaryPackage.textContent = pkgSelect?.selectedOptions?.[0]?.text.split(' — ')[0] || '—';
    if (summaryPax) summaryPax.textContent = pax || 0;
    if (summaryTotal) summaryTotal.textContent = '₱' + total.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });

    const fmt = '₱' + total.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    const g = document.getElementById('qrAmountGcash'); if (g) g.textContent = fmt;
    const m = document.getElementById('qrAmountMaya'); if (m) m.textContent = fmt;
    const c = document.getElementById('qrAmountCard'); if (c) c.textContent = fmt;
  }

  // Payment method cards -> show the matching QR panel
  document.querySelectorAll('.pm-card input[name="paymentMethod"]').forEach(radio => {
    radio.addEventListener('change', () => {
      document.querySelectorAll('.pm-card').forEach(c => c.classList.remove('selected'));
      radio.closest('.pm-card').classList.add('selected');
      const method = radio.value;
      document.querySelectorAll('.pm-qr-panel').forEach(p => p.classList.add('d-none'));
      const panel = document.getElementById('qrPanel-' + method.replace(/\s/g, ''));
      if (panel) panel.classList.remove('d-none');
    });
  });

  // Pax / package changes -> recompute totals
  document.getElementById('bookingPax')?.addEventListener('input', updateAmountSummary);
  document.getElementById('bookingPackageId')?.addEventListener('change', () => {
    const opt = document.getElementById('bookingPackageId').selectedOptions[0];
    if (opt) selectedPackageName = opt.text.split(' — ')[0];
    updateAmountSummary();
  });

  // Proof upload dropzone
  const dropzone = document.getElementById('proofDropzone');
  const fileInput = document.getElementById('bookingProofImage');
  if (dropzone && fileInput) {
    dropzone.addEventListener('click', () => fileInput.click());
    dropzone.addEventListener('dragover', (e) => { e.preventDefault(); dropzone.classList.add('dragging'); });
    dropzone.addEventListener('dragleave', () => dropzone.classList.remove('dragging'));
    dropzone.addEventListener('drop', (e) => {
      e.preventDefault();
      dropzone.classList.remove('dragging');
      if (e.dataTransfer.files.length) {
        fileInput.files = e.dataTransfer.files;
        showProofPreview();
      }
    });
    fileInput.addEventListener('change', showProofPreview);
  }

  function showProofPreview() {
    const file = fileInput?.files?.[0];
    const preview = document.getElementById('proofPreview');
    if (file && preview) {
      preview.src = URL.createObjectURL(file);
      preview.classList.remove('d-none');
    }
  }

  // Keep the two reference-number fields in sync.
  document.getElementById('bookingRefNumber')?.addEventListener('input', (e) => {
    document.getElementById('bookingRefNumber2').value = e.target.value;
  });
  document.getElementById('bookingRefNumber2')?.addEventListener('input', (e) => {
    document.getElementById('bookingRefNumber').value = e.target.value;
  });

  function populateReview() {
    const val = (id) => document.getElementById(id)?.value || '—';

    document.getElementById('reviewName').textContent = val('bookingFullName');
    const eventType = document.getElementById('bookingEventType')?.selectedOptions?.[0]?.text || 'Event';
    document.getElementById('reviewEvent').textContent = val('bookingFullName') + ' — ' + eventType;
    document.getElementById('reviewDate').textContent = val('bookingDate');
    document.getElementById('reviewPax').textContent = (parseInt(val('bookingPax'), 10) || 0) + ' guests';
    const pkgSel = document.getElementById('bookingPackageId');
    document.getElementById('reviewPackage').textContent = pkgSel?.selectedOptions?.[0]?.text.split(' — ')[0] || '—';
    document.getElementById('reviewVenue').textContent = val('bookingVenue') === '—' ? 'To be advised' : val('bookingVenue');

    const method = document.querySelector('input[name="paymentMethod"]:checked');
    document.getElementById('reviewMethod').textContent = method ? method.value : '—';
    document.getElementById('reviewRef').textContent = val('bookingRefNumber') === '—' ? '—' : val('bookingRefNumber');
    document.getElementById('reviewProof').textContent = fileInput?.files?.length ? 'Attached ✓' : 'Not attached';
    document.getElementById('reviewTotal').textContent = document.getElementById('summaryTotal')?.textContent || '₱0.00';
  }

  // Form Submission — multipart FormData (book + payment proof + chat message)
  const bookingForm = document.getElementById('clientBookingForm');
  bookingForm?.addEventListener('submit', async (e) => {
    e.preventDefault();
    const btn = document.getElementById('wizardSubmitBtn');
    if (!btn) return;

    const csrf = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
    const formData = new FormData();
    const grab = (id) => document.getElementById(id)?.value || '';

    formData.append('__RequestVerificationToken', csrf);
    formData.append('fullName', grab('bookingFullName'));
    formData.append('phone', grab('bookingPhone'));
    formData.append('eventType', grab('bookingEventType'));
    formData.append('eventDate', grab('bookingDate'));
    formData.append('paxCount', grab('bookingPax'));
    formData.append('venue', grab('bookingVenue'));
    formData.append('packageId', grab('bookingPackageId'));
    formData.append('notes', grab('bookingNotes'));

    const method = document.querySelector('input[name="paymentMethod"]:checked');
    formData.append('paymentMethod', method ? method.value : '');
    formData.append('referenceNumber', grab('bookingRefNumber'));
    formData.append('initialMessage', grab('bookingInitialMessage'));

    const proofFile = fileInput?.files?.[0];
    if (proofFile) formData.append('proofImage', proofFile);

    btn.disabled = true;
    const original = btn.innerHTML;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Confirming...';

    try {
      const resp = await fetch('/Client/Book', {
        method: 'POST',
        headers: {
          'RequestVerificationToken': csrf
        },
        body: formData
      });

      // Session expired while filling the form — ask them to sign in again.
      if (resp.status === 401 || resp.status === 403) {
        window.closeBookingModal();
        document.getElementById('signInPromptModal')?.classList.add('open');
        return;
      }

      let data = {};
      try { data = await resp.json(); } catch (_) { data = {}; }

      if (!resp.ok) {
        alert(data.error || 'Booking failed. Please try again.');
        return;
      }

      alert('Booking confirmed! Your payment is being verified — you can track and chat about your booking under My Caterings.');
      bookingForm.reset();
      window.closeBookingModal();
      if (data.bookingId) {
        window.location.href = '/Client/BookingDetails/' + data.bookingId;
      }
    } catch (err) {
      alert('Network error. Please check your connection and try again.');
    } finally {
      btn.disabled = false;
      btn.innerHTML = original;
    }
  });
}

/* ============================================================
   Payment Proof Chat (Booking Details page)
   ============================================================ */
function initPaymentChat() {
  const chatContainer = document.getElementById('paymentChatContainer');
  if (!chatContainer) return;

  window.openPaymentChat = function (proofId) {
    chatContainer.classList.add('open');
    loadChat(proofId);
  };

  window.closePaymentChat = function () {
    chatContainer.classList.remove('open');
  };

  async function loadChat(proofId) {
    const list = document.getElementById('paymentChatMessages');
    if (!list) return;
    list.innerHTML = '<div class="text-center text-muted py-4"><div class="spinner-border spinner-border-sm"></div></div>';

    try {
      const resp = await fetch('/Client/ProofMessages?proofId=' + proofId);
      if (!resp.ok) return;
      const messages = await resp.json();
      list.innerHTML = '';
      if (!messages.length) {
        list.innerHTML = '<div class="text-center text-muted small py-4">No messages yet. Say hi and tell us about your payment!</div>';
      }
      messages.forEach(m => {
        const isCustomer = m.senderRole === 'Customer';
        const div = document.createElement('div');
        div.className = 'chat-msg ' + (isCustomer ? 'chat-customer' : 'chat-admin');
        div.innerHTML = '<div class="chat-msg-name">' + m.senderName + '</div>'
          + '<div class="chat-msg-text">' + escapeHtml(m.message) + '</div>'
          + '<div class="chat-msg-time">' + m.createdAt + '</div>';
        list.appendChild(div);
      });
      list.scrollTop = list.scrollHeight;
    } catch (_) { /* ignore */ }
  }

  const sendBtn = document.getElementById('paymentChatSendBtn');
  if (sendBtn) {
    sendBtn.addEventListener('click', async () => {
      const input = document.getElementById('paymentChatInput');
      const proofId = parseInt(chatContainer.dataset.proofId || '0', 10);
      const message = input?.value?.trim();
      if (!message || !proofId) return;

      const csrf = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
      sendBtn.disabled = true;
      try {
        const form = new FormData();
        form.append('proofId', proofId);
        form.append('message', message);
        form.append('__RequestVerificationToken', csrf);
        const resp = await fetch('/Client/SendProofMessage', {
          method: 'POST',
          headers: { 'RequestVerificationToken': csrf },
          body: form
        });
        if (resp.ok) {
          input.value = '';
          await loadChat(proofId);
        }
      } finally {
        sendBtn.disabled = false;
      }
    });
  }

  // Wire View Chat buttons on the page.
  document.querySelectorAll('[data-open-proof-chat]').forEach(btn => {
    btn.addEventListener('click', () => {
      const proofId = btn.dataset.openProofChat;
      chatContainer.dataset.proofId = proofId;
      window.openPaymentChat(proofId);
    });
  });
}

function escapeHtml(str) {
  const div = document.createElement('div');
  div.textContent = str;
  return div.innerHTML;
}

/* ============================================================
   Contact / Request-a-Quote Inquiry Form
   Posts to /Client/Inquiry -> creates the CRM lead for the events team.
   ============================================================ */
function initInquiryForm() {
  const form = document.getElementById('inquiryForm');
  if (!form) return;

  const alertBox = document.getElementById('inquiryAlert');
  const successBox = document.getElementById('inquirySuccess');
  const referenceEl = document.getElementById('inquiryReference');
  const submitBtn = document.getElementById('inquirySubmitBtn');

  window.openInquiryForm = function (eventType = '') {
    const section = document.getElementById('contact');
    if (section) section.scrollIntoView({ behavior: 'smooth', block: 'start' });

    if (eventType) {
      const select = document.getElementById('inquiryEventType');
      if (select) {
        const match = Array.from(select.options).find(o => o.value === eventType);
        if (match) {
          select.value = match.value;
        } else {
          // Services cards can use labels that do not map 1:1 to the dropdown.
          select.value = 'Other';
          const other = Array.from(select.options).find(o => o.value === 'Other');
          if (other) other.textContent = `Other (${eventType})`;
        }
      }
    }

    setTimeout(() => document.getElementById('inquiryFullName')?.focus(), 350);
  };

  window.resetInquiryForm = function () {
    form.reset();
    successBox?.classList.add('d-none');
    if (referenceEl) referenceEl.textContent = '';
    if (submitBtn) submitBtn.classList.remove('d-none');
    showAlert('');
    document.getElementById('inquiryFullName')?.focus();
  };

  function showAlert(message, variant = 'danger') {
    if (!alertBox) return;
    if (!message) {
      alertBox.classList.add('d-none');
      alertBox.textContent = '';
      return;
    }
    alertBox.className = `inquiry-alert inquiry-alert-${variant}`;
    alertBox.textContent = message;
  }

  function validate() {
    const name = document.getElementById('inquiryFullName')?.value.trim();
    const email = document.getElementById('inquiryEmail')?.value.trim();
    const eventType = document.getElementById('inquiryEventType')?.value;
    const message = document.getElementById('inquiryMessage')?.value.trim();

    if (!name) return 'Please enter your full name.';
    if (!email) return 'Please enter your email address.';
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) return 'Please enter a valid email address.';
    if (!eventType) return 'Please choose an event type.';
    if (!message) return 'Please tell us about your event.';
    return null;
  }

  form.addEventListener('submit', async (e) => {
    e.preventDefault();

    const validationError = validate();
    if (validationError) {
      showAlert(validationError);
      return;
    }

    const csrf = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
    const grab = (id) => document.getElementById(id)?.value?.trim() || '';

    const payload = new FormData();
    payload.append('__RequestVerificationToken', csrf);
    payload.append('fullName', grab('inquiryFullName'));
    payload.append('email', grab('inquiryEmail'));
    payload.append('phone', grab('inquiryPhone'));
    payload.append('company', grab('inquiryCompany'));
    payload.append('eventType', grab('inquiryEventType'));
    payload.append('eventDate', grab('inquiryEventDate'));
    payload.append('paxCount', grab('inquiryPax') || '0');
    payload.append('venue', grab('inquiryVenue'));
    payload.append('packageId', grab('inquiryPackageId'));
    payload.append('message', grab('inquiryMessage'));
    payload.append('source', window.__CATERINGFLOW_INQUIRY_SOURCE__ || 'Website');

    const originalHtml = submitBtn ? submitBtn.innerHTML : '';
    if (submitBtn) {
      submitBtn.disabled = true;
      submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Sending...';
    }
    showAlert('');

    try {
      const resp = await fetch('/Client/Inquiry', {
        method: 'POST',
        headers: { 'RequestVerificationToken': csrf },
        body: payload
      });

      let data = {};
      try { data = await resp.json(); } catch (_) { data = {}; }

      if (!resp.ok) {
        showAlert(data.error || 'We could not send your inquiry. Please try again.');
        return;
      }

      if (referenceEl) referenceEl.textContent = data.reference || '—';
      successBox?.classList.remove('d-none');
      if (submitBtn) submitBtn.classList.add('d-none');
    } catch (err) {
      showAlert('Network error. Please check your connection and try again.');
    } finally {
      if (submitBtn) {
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalHtml;
      }
    }
  });
}