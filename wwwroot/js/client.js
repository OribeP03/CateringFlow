/* ============================================================
   CateringFlow - Client JS
   ============================================================ */

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
  window.toggleMobileNav = function() {
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
  window.closeSignInPrompt = function() {
    const modal = document.getElementById('signInPromptModal');
    if (modal) modal.classList.remove('open');
  };

  // Modal Open/Close handlers
  window.openBookingModal = function(packageName = '') {
    const isAuthenticated = window.__CATERINGFLOW_AUTH__ === 'true';

    // Signed-out visitors must sign in before booking.
    if (!isAuthenticated) {
      const prompt = document.getElementById('signInPromptModal');
      if (prompt) prompt.classList.add('open');
      return;
    }

    const modal = document.getElementById('bookingModal');
    if (modal) {
      modal.classList.add('open');
      if (packageName) {
        const pkgSelect = document.getElementById('bookingPackageSelect');
        if (pkgSelect) pkgSelect.value = packageName;
      }
    }
  };

  window.closeBookingModal = function() {
    const modal = document.getElementById('bookingModal');
    if (modal) modal.classList.remove('open');
  };

  // Close sign-in prompt when clicking outside
  document.getElementById('signInPromptModal')?.addEventListener('click', (e) => {
    if (e.target.id === 'signInPromptModal') {
      window.closeSignInPrompt();
    }
  });

  // Close modal when clicking outside
  document.getElementById('bookingModal')?.addEventListener('click', (e) => {
    if (e.target.id === 'bookingModal') {
      window.closeBookingModal();
    }
  });

  // Form Submission — persist the booking to the server via /Client/Book
  const bookingForm = document.getElementById('clientBookingForm');
  bookingForm?.addEventListener('submit', async (e) => {
    e.preventDefault();
    const btn = bookingForm.querySelector('button[type="submit"]');
    if (!btn) return;

    const csrf = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
    const payload = {
      fullName: document.getElementById('bookingFullName')?.value || '',
      phone: document.getElementById('bookingPhone')?.value || '',
      eventType: document.getElementById('bookingEventType')?.value || 'Wedding',
      eventDate: document.getElementById('bookingDate')?.value || null,
      paxCount: parseInt(document.getElementById('bookingPax')?.value || '0', 10) || 0,
      venue: document.getElementById('bookingVenue')?.value || '',
      packageName: document.getElementById('bookingPackageSelect')?.value || '',
      notes: document.getElementById('bookingNotes')?.value || ''
    };

    btn.disabled = true;
    const original = btn.innerHTML;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Submitting...';

    try {
      const resp = await fetch('/Client/Book', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'RequestVerificationToken': csrf
        },
        body: JSON.stringify(payload)
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

      alert('Booking request received! Our concierge will contact you within 24 hours. You can track it under My Caterings.');
      bookingForm.reset();
      window.closeBookingModal();
    } catch (err) {
      alert('Network error. Please check your connection and try again.');
    } finally {
      btn.disabled = false;
      btn.innerHTML = original;
    }
  });
});
