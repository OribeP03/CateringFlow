/* ============================================================
   CateringFlow - Client JS
   ============================================================ */

document.addEventListener('DOMContentLoaded', () => {
  // Sticky Navbar on Scroll
  const navbar = document.querySelector('.client-navbar');
  window.addEventListener('scroll', () => {
    if (window.scrollY > 40) {
      navbar?.classList.add('scrolled');
    } else {
      navbar?.classList.remove('scrolled');
    }
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

  // Form Submission
  const bookingForm = document.getElementById('clientBookingForm');
  bookingForm?.addEventListener('submit', (e) => {
    e.preventDefault();
    alert('Thank you! Your catering inquiry has been received. Our concierge will contact you within 24 hours with your tailored proposal.');
    window.closeBookingModal();
    bookingForm.reset();
  });
});
