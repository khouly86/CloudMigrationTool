# PowerShell Script to Create Static Files
# Step 8: Create CSS, JavaScript, and other static resources

param(
    [string]$SolutionName = "CloudMigrationTool"
)

Write-Host "Creating Static Files..." -ForegroundColor Green

# Ensure we're in the solution directory
if (!(Test-Path "$SolutionName.sln")) {
    Write-Error "Solution file not found. Please run previous setup scripts first."
    exit 1
}

Write-Host "Creating CSS Files..." -ForegroundColor Cyan

# Custom Site CSS
@"
/* Cloud Migration Tool Custom Styles */

:root {
    --primary-color: #0d6efd;
    --secondary-color: #6c757d;
    --success-color: #198754;
    --info-color: #0dcaf0;
    --warning-color: #ffc107;
    --danger-color: #dc3545;
    --light-color: #f8f9fa;
    --dark-color: #212529;
}

body {
    background-color: #f8f9fa;
    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
}

/* Navigation Improvements */
.navbar-brand {
    font-weight: bold;
    font-size: 1.5rem;
}

.navbar-nav .nav-link {
    font-weight: 500;
    transition: color 0.3s ease;
}

.navbar-nav .nav-link:hover {
    color: rgba(255, 255, 255, 0.8) !important;
}

/* Card Enhancements */
.card {
    border: none;
    border-radius: 12px;
    box-shadow: 0 0.125rem 0.25rem rgba(0, 0, 0, 0.075);
    transition: box-shadow 0.15s ease-in-out;
}

.card:hover {
    box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15);
}

.card-header {
    background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
    border-bottom: 1px solid #dee2e6;
    border-radius: 12px 12px 0 0 !important;
    font-weight: 600;
}

/* Dashboard Stats Cards */
.card.bg-primary,
.card.bg-success,
.card.bg-danger,
.card.bg-info,
.card.bg-warning {
    background: linear-gradient(135deg, var(--primary-color) 0%, #0a58ca 100%) !important;
    border: none;
    transform: translateY(0);
    transition: transform 0.2s ease-in-out;
}

.card.bg-primary:hover,
.card.bg-success:hover,
.card.bg-danger:hover,
.card.bg-info:hover,
.card.bg-warning:hover {
    transform: translateY(-2px);
}

.card.bg-success {
    background: linear-gradient(135deg, var(--success-color) 0%, #146c43 100%) !important;
}

.card.bg-danger {
    background: linear-gradient(135deg, var(--danger-color) 0%, #b02a37 100%) !important;
}

.card.bg-info {
    background: linear-gradient(135deg, var(--info-color) 0%, #0aa2c0 100%) !important;
}

.card.bg-warning {
    background: linear-gradient(135deg, var(--warning-color) 0%, #d39e00 100%) !important;
    color: #000 !important;
}

/* Progress Bars */
.progress {
    border-radius: 10px;
    background-color: #e9ecef;
    overflow: hidden;
}

.progress-bar {
    border-radius: 10px;
    background: linear-gradient(90deg, var(--primary-color) 0%, #0aa2c0 100%);
    transition: width 0.6s ease;
}

.progress-bar-striped {
    background-image: linear-gradient(45deg, rgba(255, 255, 255, 0.15) 25%, transparent 25%, transparent 50%, rgba(255, 255, 255, 0.15) 50%, rgba(255, 255, 255, 0.15) 75%, transparent 75%, transparent);
}

/* Badges */
.badge {
    font-size: 0.75em;
    padding: 0.35em 0.65em;
    border-radius: 6px;
    font-weight: 600;
}

/* Buttons */
.btn {
    border-radius: 8px;
    font-weight: 500;
    transition: all 0.2s ease-in-out;
    position: relative;
    overflow: hidden;
}

.btn::before {
    content: '';
    position: absolute;
    top: 0;
    left: -100%;
    width: 100%;
    height: 100%;
    background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.2), transparent);
    transition: left 0.5s;
}

.btn:hover::before {
    left: 100%;
}

.btn-primary {
    background: linear-gradient(135deg, var(--primary-color) 0%, #0a58ca 100%);
    border: none;
}

.btn-success {
    background: linear-gradient(135deg, var(--success-color) 0%, #146c43 100%);
    border: none;
}

.btn-danger {
    background: linear-gradient(135deg, var(--danger-color) 0%, #b02a37 100%);
    border: none;
}

/* Tables */
.table {
    border-radius: 8px;
    overflow: hidden;
}

.table th {
    background-color: #f8f9fa;
    border-top: none;
    font-weight: 600;
    color: var(--dark-color);
    padding: 1rem 0.75rem;
}

.table td {
    vertical-align: middle;
    padding: 0.75rem;
}

.table-hover tbody tr:hover {
    background-color: rgba(0, 123, 255, 0.05);
}

/* Forms */
.form-control, .form-select {
    border-radius: 8px;
    border: 1px solid #ced4da;
    transition: border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out;
}

.form-control:focus, .form-select:focus {
    border-color: var(--primary-color);
    box-shadow: 0 0 0 0.2rem rgba(13, 110, 253, 0.25);
}

/* Alerts */
.alert {
    border: none;
    border-radius: 8px;
    padding: 1rem 1.25rem;
}

.alert-success {
    background: linear-gradient(135deg, #d4edda 0%, #c3e6cb 100%);
    color: #155724;
}

.alert-danger {
    background: linear-gradient(135deg, #f8d7da 0%, #f5c6cb 100%);
    color: #721c24;
}

.alert-info {
    background: linear-gradient(135deg, #cce7ff 0%, #b3d9ff 100%);
    color: #0c5460;
}

/* Status Indicators */
.status-online {
    color: var(--success-color);
    animation: pulse 2s infinite;
}

.status-offline {
    color: var(--danger-color);
}

.status-warning {
    color: var(--warning-color);
    animation: pulse 2s infinite;
}

@keyframes pulse {
    0% { opacity: 1; }
    50% { opacity: 0.5; }
    100% { opacity: 1; }
}

/* Loading Spinner */
.spinner-border-sm {
    width: 1rem;
    height: 1rem;
}

/* Modal Improvements */
.modal-content {
    border-radius: 12px;
    border: none;
    box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15);
}

.modal-header {
    background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
    border-bottom: 1px solid #dee2e6;
    border-radius: 12px 12px 0 0;
}

/* Footer */
footer {
    margin-top: auto;
    border-top: 1px solid #dee2e6;
}

/* Responsive Improvements */
@media (max-width: 768px) {
    .card {
        margin-bottom: 1rem;
    }
    
    .btn-group .btn {
        padding: 0.375rem 0.5rem;
    }
    
    .table-responsive {
        border-radius: 8px;
    }
    
    h1 {
        font-size: 1.5rem;
    }
}

/* Custom Utility Classes */
.text-gradient-primary {
    background: linear-gradient(135deg, var(--primary-color) 0%, #0aa2c0 100%);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
    background-clip: text;
    font-weight: bold;
}

.shadow-soft {
    box-shadow: 0 0.125rem 0.25rem rgba(0, 0, 0, 0.075) !important;
}

.shadow-hover:hover {
    box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15) !important;
    transform: translateY(-1px);
}

/* Connection Status Icons */
.connection-status-connected {
    color: var(--success-color);
}

.connection-status-error {
    color: var(--danger-color);
}

.connection-status-testing {
    color: var(--warning-color);
    animation: spin 1s linear infinite;
}

@keyframes spin {
    from { transform: rotate(0deg); }
    to { transform: rotate(360deg); }
}

/* Job Progress Animations */
.job-progress-container {
    position: relative;
    overflow: hidden;
    border-radius: 8px;
}

.job-progress-bar {
    height: 6px;
    background: linear-gradient(90deg, var(--success-color) 0%, var(--info-color) 100%);
    transition: width 0.8s ease;
}

/* Dashboard Grid */
.dashboard-grid {
    display: grid;
    gap: 1.5rem;
    grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
}

@media (min-width: 992px) {
    .dashboard-grid {
        grid-template-columns: 2fr 1fr;
    }
}

/* Empty States */
.empty-state {
    text-align: center;
    padding: 3rem 1rem;
    color: var(--secondary-color);
}

.empty-state i {
    font-size: 4rem;
    margin-bottom: 1rem;
    opacity: 0.5;
}

/* Migration Type Icons */
.migration-type-ad::before {
    content: '👥';
}

.migration-type-exchange::before {
    content: '📧';
}

.migration-type-onedrive::before {
    content: '☁️';
}

.migration-type-sharepoint::before {
    content: '🗂️';
}
"@ | Out-File -FilePath "src\CloudMigrationTool.Web\wwwroot\css\site.css" -Force -Encoding UTF8

Write-Host "Creating JavaScript Files..." -ForegroundColor Cyan

# Custom Site JavaScript
@"
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
function showNotification(message, type = 'info', duration = 5000) {
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
function confirmDelete(title, text, confirmButtonText = 'Delete') {
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
function formatBytes(bytes, decimals = 2) {
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
function exportTableAsCSV(tableId, filename = 'export.csv') {
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
"@ | Out-File -FilePath "src\CloudMigrationTool.Web\wwwroot\js\site.js" -Force -Encoding UTF8

Write-Host "Downloading Bootstrap and jQuery..." -ForegroundColor Cyan

# Create CDN download script for Bootstrap CSS
$bootstrapCss = Invoke-RestMethod -Uri "https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css"
$bootstrapCss | Out-File -FilePath "src\CloudMigrationTool.Web\wwwroot\css\bootstrap.min.css" -Encoding UTF8

# Create CDN download script for Bootstrap JS
$bootstrapJs = Invoke-RestMethod -Uri "https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"
$bootstrapJs | Out-File -FilePath "src\CloudMigrationTool.Web\wwwroot\js\bootstrap.bundle.min.js" -Encoding UTF8

# Create CDN download script for jQuery
$jqueryJs = Invoke-RestMethod -Uri "https://code.jquery.com/jquery-3.6.0.min.js"
$jqueryJs | Out-File -FilePath "src\CloudMigrationTool.Web\wwwroot\js\jquery-3.6.0.min.js" -Encoding UTF8

Write-Host "Creating Favicon and Images..." -ForegroundColor Cyan

# Create a simple favicon.ico placeholder (base64 encoded 16x16 icon)
$faviconBytes = [System.Convert]::FromBase64String("AAABAAEAEBAAAAEAIABoBAAAFgAAACgAAAAQAAAAIAAAAAEAIAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA4ODg/+Dg4P/g4OD/4ODg/+Dg4P/g4OD/4ODg/+Dg4P/g4OD/4ODg/wAAAAAAAAAAAAAAAAAAAAAAAAAA4ODg/+Dg4P/g4OD/4ODg/+Dg4P/g4OD/4ODg/+Dg4P/g4OD/4ODg/wAAAAAAAAAAAAAAAAAAAAAAAAAA4ODg/+Dg4P/g4OD/4ODg/+Dg4P/g4OD/4ODg/+Dg4P/g4OD/4ODg/wAAAAAAAAAAAAAAAAAAAAAAAAAA4ODg/+Dg4P/g4OD/4ODg/+Dg4P/g4OD/4ODg/+Dg4P/g4OD/4ODg/wAAAAAAAAAAAAAAAAAAAAAAAAAA4ODg/+Dg4P/g4OD/4ODg/+Dg4P/g4OD/4ODg/+Dg4P/g4OD/4ODg/wAAAAAAAAAAAAAAAAAAAAAAAAAA4ODg/+Dg4P/g4OD/4ODg/+Dg4P/g4OD/4ODg/+Dg4P/g4OD/4ODg/wAAAAAAAAAAAAAAAAAAAAAAAAAA4ODg/+Dg4P/g4OD/4ODg/+Dg4P/g4OD/4ODg/+Dg4P/g4OD/4ODg/wAAAAAAAAAAAAAAAAAAAAAAAAAA4ODg/+Dg4P/g4OD/4ODg/+Dg4P/g4OD/4ODg/+Dg4P/g4OD/4ODg/wAAAAAAAAAAAAAAAAAAAAAAAAAA4ODg/+Dg4P/g4OD/4ODg/+Dg4P/g4OD/4ODg/+Dg4P/g4OD/4ODg/wAAAAAAAAAAAAAAAAAAAAAAAAAA4ODg/+Dg4P/g4OD/4ODg/+Dg4P/g4OD/4ODg/+Dg4P/g4OD/4ODg/wAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAD//wAA//8AAP//AAD//wAA//8AAP//AAD//wAA//8AAP//AAD//wAA//8AAP//AAD//wAA//8AAP//AAD//wAA")
[System.IO.File]::WriteAllBytes("src\CloudMigrationTool.Web\wwwroot\favicon.ico", $faviconBytes)

Write-Host "`nStatic files created successfully!" -ForegroundColor Green
Write-Host "Next: Run 09-Create-Program-And-Startup.ps1 to create the startup configuration" -ForegroundColor Yellow