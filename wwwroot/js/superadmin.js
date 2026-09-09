/* ============================================================
   CateringFlow - SuperAdmin ERP + CRM Interactive JS
   ============================================================ */

document.addEventListener('DOMContentLoaded', () => {
  initDashboardCharts();
  initTableFilters();
  initViewTogglers();
});

// 1. Dashboard Charts (Chart.js)
function initDashboardCharts() {
  const revCtx = document.getElementById('monthlyRevenueChart');
  if (revCtx && typeof Chart !== 'undefined') {
    const gradient = revCtx.getContext('2d').createLinearGradient(0, 0, 0, 220);
    gradient.addColorStop(0, 'rgba(184, 115, 51, 0.35)');
    gradient.addColorStop(1, 'rgba(184, 115, 51, 0.01)');

    new Chart(revCtx, {
      type: 'line',
      data: {
        labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep'],
        datasets: [{
          label: 'Revenue',
          data: [220, 250, 310, 305, 420, 920, 600, 520, 440],
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
          x: {
            grid: { display: false }
          },
          y: {
            grid: { color: '#f0ebe2' },
            ticks: {
              callback: (val) => `₱${val}k`
            }
          }
        }
      }
    });
  }

  const eventsCtx = document.getElementById('eventsByTypeChart');
  if (eventsCtx && typeof Chart !== 'undefined') {
    new Chart(eventsCtx, {
      type: 'bar',
      data: {
        labels: ['Wedding', 'Corporate', 'Birthday', 'Anniversary', 'Other'],
        datasets: [{
          data: [20, 34, 18, 11, 8],
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

  // Reports Page Charts
  const reportRevCtx = document.getElementById('reportRevenueOverviewChart');
  if (reportRevCtx && typeof Chart !== 'undefined') {
    const gradient = reportRevCtx.getContext('2d').createLinearGradient(0, 0, 0, 220);
    gradient.addColorStop(0, 'rgba(184, 115, 51, 0.35)');
    gradient.addColorStop(1, 'rgba(184, 115, 51, 0.01)');

    new Chart(reportRevCtx, {
      type: 'line',
      data: {
        labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep'],
        datasets: [{
          label: 'Revenue (₱k)',
          data: [220, 250, 310, 305, 420, 920, 600, 520, 440],
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

  const reportEventsCtx = document.getElementById('reportEventsByTypeChart');
  if (reportEventsCtx && typeof Chart !== 'undefined') {
    new Chart(reportEventsCtx, {
      type: 'bar',
      data: {
        labels: ['Wedding', 'Corporate', 'Birthday', 'Anniversary', 'Other'],
        datasets: [{
          data: [18, 34, 13, 8, 5],
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
