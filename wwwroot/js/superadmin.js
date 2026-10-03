/* ============================================================
   CateringFlow - SuperAdmin ERP + CRM Interactive JS
   ============================================================ */

document.addEventListener('DOMContentLoaded', () => {
  initDashboardCharts();
  initTableFilters();
  initViewTogglers();
});

// 1. Dashboard & Reports Charts (Chart.js, data comes from canvas data-* attributes)
function initDashboardCharts() {
  if (typeof Chart === 'undefined') return;

  // Monthly revenue line chart (values in pesos -> shown in thousands)
  const revCtx = document.getElementById('monthlyRevenueChart');
  if (revCtx) {
    const labels = JSON.parse(revCtx.dataset.labels || '[]');
    const raw = JSON.parse(revCtx.dataset.values || '[]');
    const values = raw.map(v => Math.round((v / 1000) * 100) / 100);

    const gradient = revCtx.getContext('2d').createLinearGradient(0, 0, 0, 220);
    gradient.addColorStop(0, 'rgba(184, 115, 51, 0.35)');
    gradient.addColorStop(1, 'rgba(184, 115, 51, 0.01)');

    new Chart(revCtx, {
      type: 'line',
      data: {
        labels: labels,
        datasets: [{
          label: 'Revenue',
          data: values,
          borderColor: '#b87333',
          borderWidth: 3,
          tension: 0.4,
          fill: true,
          backgroundColor: gradient,
          pointRadius: 3,
          pointBackgroundColor: '#b87333',
          pointHoverRadius: 6
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            callbacks: {
              label: (ctx) => ` ₱${ctx.parsed.y}k`
            }
          }
        },
        scales: {
          x: { grid: { display: false } },
          y: {
            grid: { color: '#f0ebe2' },
            ticks: { callback: (val) => `₱${val}k` }
          }
        }
      }
    });
  }

  // Events by type horizontal bar
  const eventsCtx = document.getElementById('eventsByTypeChart');
  if (eventsCtx) {
    const labels = JSON.parse(eventsCtx.dataset.labels || '[]');
    const values = JSON.parse(eventsCtx.dataset.values || '[]');

    new Chart(eventsCtx, {
      type: 'bar',
      data: {
        labels: labels,
        datasets: [{
          data: values,
          backgroundColor: '#b87333',
          borderRadius: 6,
          barThickness: 16
        }]
      },
      options: {
        indexAxis: 'y',
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
          x: { grid: { color: '#f0ebe2' } },
          y: { grid: { display: false } }
        }
      }
    });
  }

  // Reports Page: Revenue Overview (same dataset-driven pattern)
  const reportRevCtx = document.getElementById('reportRevenueOverviewChart');
  if (reportRevCtx) {
    const labels = JSON.parse(reportRevCtx.dataset.labels || '[]');
    const raw = JSON.parse(reportRevCtx.dataset.values || '[]');
    const values = raw.map(v => Math.round((v / 1000) * 100) / 100);

    const gradient = reportRevCtx.getContext('2d').createLinearGradient(0, 0, 0, 220);
    gradient.addColorStop(0, 'rgba(184, 115, 51, 0.35)');
    gradient.addColorStop(1, 'rgba(184, 115, 51, 0.01)');

    new Chart(reportRevCtx, {
      type: 'line',
      data: {
        labels: labels,
        datasets: [{
          label: 'Revenue (₱k)',
          data: values,
          borderColor: '#b87333',
          borderWidth: 3,
          tension: 0.4,
          fill: true,
          backgroundColor: gradient,
          pointRadius: 3,
          pointBackgroundColor: '#b87333'
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
          x: { grid: { display: false } },
          y: {
            grid: { color: '#f0ebe2' },
            ticks: { callback: (val) => `₱${val}k` }
          }
        }
      }
    });
  }

  // Reports Page: Events by type
  const reportEventsCtx = document.getElementById('reportEventsByTypeChart');
  if (reportEventsCtx) {
    const labels = JSON.parse(reportEventsCtx.dataset.labels || '[]');
    const values = JSON.parse(reportEventsCtx.dataset.values || '[]');

    new Chart(reportEventsCtx, {
      type: 'bar',
      data: {
        labels: labels,
        datasets: [{
          data: values,
          backgroundColor: '#b87333',
          borderRadius: 6,
          barThickness: 28
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
          x: { grid: { display: false } },
          y: { grid: { color: '#f0ebe2' } }
        }
      }
    });
  }
}

// 2. View Togglers (List vs Calendar)
function initViewTogglers() {
  const listBtn = document.getElementById('btnListView');
  const calBtn = document.getElementById('btnCalView');
  const tableContainer = document.getElementById('eventsTableContainer');
  const calendarContainer = document.getElementById('eventsCalendarContainer');

  if (listBtn && calBtn && tableContainer && calendarContainer) {
    listBtn.addEventListener('click', () => {
      listBtn.classList.remove('admin-btn-secondary');
      listBtn.classList.add('admin-btn-primary');
      calBtn.classList.remove('admin-btn-primary');
      calBtn.classList.add('admin-btn-secondary');
      tableContainer.style.display = 'block';
      calendarContainer.style.display = 'none';
    });

    calBtn.addEventListener('click', () => {
      calBtn.classList.remove('admin-btn-secondary');
      calBtn.classList.add('admin-btn-primary');
      listBtn.classList.remove('admin-btn-primary');
      listBtn.classList.add('admin-btn-secondary');
      tableContainer.style.display = 'none';
      calendarContainer.style.display = 'block';
    });
  }
}

// 3. Search and Dropdown Filter Functionality
function initTableFilters() {
  const searchInput = document.getElementById('tableLiveSearch');
  const typeFilter = document.getElementById('tableTypeFilter');
  const statusFilter = document.getElementById('tableStatusFilter');
  const table = document.querySelector('.admin-data-table tbody');

  if (!table) return;

  function filterRows() {
    const q = (searchInput?.value || '').toLowerCase().trim();
    const typeVal = (typeFilter?.value || 'All').toLowerCase();
    const statusVal = (statusFilter?.value || 'All').toLowerCase();

    const rows = table.querySelectorAll('tr');
    rows.forEach(row => {
      const text = row.innerText.toLowerCase();
      const matchesSearch = !q || text.includes(q);
      const matchesType = typeVal === 'all' || text.includes(typeVal);
      const matchesStatus = statusVal === 'all' || text.includes(statusVal);

      if (matchesSearch && matchesType && matchesStatus) {
        row.style.display = '';
      } else {
        row.style.display = 'none';
      }
    });
  }

  searchInput?.addEventListener('input', filterRows);
  typeFilter?.addEventListener('change', filterRows);
  statusFilter?.addEventListener('change', filterRows);
}

// 4. Modal Open / Close Helpers
window.openAdminModal = function(modalId) {
  const modal = document.getElementById(modalId);
  if (modal) modal.classList.add('open');
};

window.closeAdminModal = function(modalId) {
  const modal = document.getElementById(modalId);
  if (modal) modal.classList.remove('open');
};

// 5. Global Action Helpers
window.handleDeleteRow = function(id) {
  if (confirm(`Are you sure you want to delete item ${id}?`)) {
    alert(`Item ${id} deleted successfully.`);
  }
};

window.handleDemoRoleChange = function(selectElem) {
  alert(`Role switched to: ${selectElem.value}. Permissions adjusted.`);
};
