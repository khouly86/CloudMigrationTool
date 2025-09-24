// Cloud Migration Tool JavaScript Functions

$(document).ready(function () {
    // Initialize tooltips
    initializeTooltips();
    
    // Initialize auto-refresh functionality
    initializeAutoRefresh();
    
    // Initialize form validation
    initializeFormValidation();
    
    // Initialize connection testing
    initializeConnectionTesting();
    
    // Initialize migration progress tracking
    initializeMigrationProgress();
});

// Initialize Bootstrap tooltips
function initializeTooltips() {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

// Auto-refresh functionality for dashboard pages
function initializeAutoRefresh() {
    const refreshInterval = 30000; // 30 seconds
    const currentPage = window.location.pathname.toLowerCase();
    
    // Only auto-refresh on dashboard pages
    if (currentPage.includes('dashboard') || currentPage.includes('migration')) {
        setInterval(function() {
            // Check if there are any active migrations
            const activeJobs = document.querySelectorAll('.progress-bar-animated');
            if (activeJobs.length > 0) {
                refreshPage();
            }
        }, refreshInterval);
    }
}

// Refresh page with fade effect
function refreshPage() {
    document.body.style.opacity = '0.8';
    setTimeout(function() {
        location.reload();
    }, 500);
}

// Form validation enhancements
function initializeFormValidation() {
    // Add custom validation styles
    const forms = document.querySelectorAll('.needs-validation');
    
    Array.prototype.slice.call(forms).forEach(function(form) {
        form.addEventListener('submit', function(event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
                
                // Focus on first invalid field
                const firstInvalidField = form.querySelector(':invalid');
                if (firstInvalidField) {
                    firstInvalidField.focus();
                }
            }
            
            form.classList.add('was-validated');
        }, false);
    });
}

// Connection testing functionality
function initializeConnectionTesting() {
    $(document).on('click', '.test-connection', function(e) {
        e.preventDefault();
        
        const button = $(this);
        const connectionId = button.data('id');
        const originalHtml = button.html();
        
        // Disable button and show spinner
        button.prop('disabled', true)
              .removeClass('btn-outline-primary btn-outline-success btn-outline-danger')
              .addClass('btn-outline-secondary')
              .html('<i class="fas fa-spinner fa-spin"></i> Testing...');
        
        // Make AJAX request
        $.ajax({
            url: '/Admin/TestConnection',
            type: 'POST',
            data: { id: connectionId },
            success: function(response) {
                if (response.success) {
                    button.removeClass('btn-outline-secondary')
                          .addClass('btn-outline-success')
                          .html('<i class="fas fa-check"></i> Connected');
                    
                    showNotification('Connection test successful!', 'success');
                } else {
                    button.removeClass('btn-outline-secondary')
                          .addClass('btn-outline-danger')
                          .html('<i class="fas fa-times"></i> Failed');
                    
                    showNotification('Connection test failed: ' + response.message, 'danger');
                }
            },
            error: function(xhr, status, error) {
                button.removeClass('btn-outline-secondary')
                      .addClass('btn-outline-danger')
                      .html('<i class="fas fa-exclamation-triangle"></i> Error');
                
                showNotification('Connection test error: ' + error, 'danger');
            },
            complete: function() {
                // Reset button after 3 seconds
                setTimeout(function() {
                    button.prop('disabled', false)
                          .removeClass('btn-outline-success btn-outline-danger btn-outline-secondary')
                          .addClass('btn-outline-primary')
                          .html(originalHtml);
                }, 3000);
            }
        });
    });
}

// Migration progress tracking
function initializeMigrationProgress() {
    const progressElements = document.querySelectorAll('[data-job-id]');
    
    progressElements.forEach(function(element) {
        const jobId = element.getAttribute('data-job-id');
        if (jobId) {
            updateMigrationProgress(jobId);
        }
    });
}

// Update migration progress for a specific job
function updateMigrationProgress(jobId) {
    $.ajax({
        url: '/Migration/GetProgress/' + jobId,
        type: 'GET',
        success: function(progress) {
            updateProgressDisplay(jobId, progress);
        },
        error: function(xhr, status, error) {
            console.error('Failed to get progress for job ' + jobId + ': ' + error);
        }
    });
}

// Update progress display elements
function updateProgressDisplay(jobId, progress) {
    const progressBar = document.querySelector(`[data-job-id="${jobId}"] .progress-bar`);
    const progressText = document.querySelector(`[data-job-id="${jobId}"] .progress-text`);
    const itemCount = document.querySelector(`[data-job-id="${jobId}"] .item-count`);
    
    if (progressBar) {
        progressBar.style.width = progress.percentageComplete + '%';
        progressBar.setAttribute('aria-valuenow', progress.percentageComplete);
        progressBar.textContent = progress.percentageComplete.toFixed(1) + '%';
    }
    
    if (progressText && progress.currentOperation) {
        progressText.textContent = progress.currentOperation;
    }
    
    if (itemCount) {
        itemCount.textContent = `${progress.processedItems} / ${progress.totalItems} items`;
    }
    
    // Show error if present
    if (progress.lastError && progress.lastError.trim() !== '') {
        showNotification('Migration error: ' + progress.lastError, 'warning');
    }
}

// Show notification toast
function showNotification(message, type, duration) {
    type = type || 'info';
    duration = duration || 5000;
    
    const toastHtml = `
        <div class="toast align-items-center text-white bg-${type} border-0" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    `;
    
    // Create toast container if it doesn't exist
    let toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toast-container';
        toastContainer.className = 'position-fixed top-0 end-0 p-3';
        toastContainer.style.zIndex = '9999';
        document.body.appendChild(toastContainer);
    }
    
    // Add toast to container
    toastContainer.insertAdjacentHTML('beforeend', toastHtml);
    
    // Show toast
    const toastElement = toastContainer.lastElementChild;
    const toast = new bootstrap.Toast(toastElement, {
        autohide: true,
        delay: duration
    });
    
    toast.show();
    
    // Remove toast element after it's hidden
    toastElement.addEventListener('hidden.bs.toast', function() {
        toastElement.remove();
    });
}

// Confirm deletion with sweet alert-style modal
function confirmDelete(title, text, confirmButtonText) {
    confirmButtonText = confirmButtonText || 'Delete';
    
    return new Promise((resolve) => {
        const modalHtml = `
            <div class="modal fade" id="confirmDeleteModal" tabindex="-1">
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content">
                        <div class="modal-header border-0">
                            <h5 class="modal-title text-danger">
                                <i class="fas fa-exclamation-triangle"></i> ${title}
                            </h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <p>${text}</p>
                        </div>
                        <div class="modal-footer border-0">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                            <button type="button" class="btn btn-danger" id="confirmDeleteBtn">${confirmButtonText}</button>
                        </div>
                    </div>
                </div>
            </div>
        `;
        
        // Remove existing modal if present
        const existingModal = document.getElementById('confirmDeleteModal');
        if (existingModal) {
            existingModal.remove();
        }
        
        // Add modal to body
        document.body.insertAdjacentHTML('beforeend', modalHtml);
        
        const modal = new bootstrap.Modal(document.getElementById('confirmDeleteModal'));
        
        // Handle confirm button click
        document.getElementById('confirmDeleteBtn').addEventListener('click', function() {
            modal.hide();
            resolve(true);
        });
        
        // Handle modal dismiss
        document.getElementById('confirmDeleteModal').addEventListener('hidden.bs.modal', function() {
            document.getElementById('confirmDeleteModal').remove();
            resolve(false);
        });
        
        modal.show();
    });
}

// Format bytes to human readable format
function formatBytes(bytes, decimals) {
    decimals = decimals || 2;
    if (bytes === 0) return '0 Bytes';
    
    const k = 1024;
    const dm = decimals < 0 ? 0 : decimals;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];
    
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
}

// Format time duration
function formatDuration(milliseconds) {
    const seconds = Math.floor(milliseconds / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);
    
    if (days > 0) return `${days}d ${hours % 24}h`;
    if (hours > 0) return `${hours}h ${minutes % 60}m`;
    if (minutes > 0) return `${minutes}m ${seconds % 60}s`;
    return `${seconds}s`;
}

// Copy text to clipboard
function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(function() {
        showNotification('Copied to clipboard!', 'success');
    }).catch(function(err) {
        console.error('Failed to copy text: ', err);
        showNotification('Failed to copy text', 'danger');
    });
}

// Export table data as CSV
function exportTableAsCSV(tableId, filename) {
    filename = filename || 'export.csv';
    const table = document.getElementById(tableId);
    if (!table) return;
    
    let csv = [];
    const rows = table.querySelectorAll('tr');
    
    for (let i = 0; i < rows.length; i++) {
        const row = [];
        const cols = rows[i].querySelectorAll('td, th');
        
        for (let j = 0; j < cols.length; j++) {
            row.push(cols[j].innerText);
        }
        
        csv.push(row.join(','));
    }
    
    const csvFile = new Blob([csv.join('\n')], { type: 'text/csv' });
    const downloadLink = document.createElement('a');
    
    downloadLink.download = filename;
    downloadLink.href = window.URL.createObjectURL(csvFile);
    downloadLink.style.display = 'none';
    
    document.body.appendChild(downloadLink);
    downloadLink.click();
    document.body.removeChild(downloadLink);
}

// Debounce function for search inputs
function debounce(func, wait, immediate) {
    let timeout;
    return function executedFunction() {
        const context = this;
        const args = arguments;
        
        const later = function() {
            timeout = null;
            if (!immediate) func.apply(context, args);
        };
        
        const callNow = immediate && !timeout;
        
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
        
        if (callNow) func.apply(context, args);
    };
}
