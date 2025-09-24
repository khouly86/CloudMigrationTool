// Exploration functionality for item selection and filtering
$(document).ready(function() {
    initializeExploration();
});

function initializeExploration() {
    // Initialize tooltips
    $('[data-bs-toggle="tooltip"]').tooltip();

    // Initialize search functionality
    $('#searchQuery').on('input', debounce(performSearch, 300));

    // Initialize checkbox functionality
    initializeCheckboxes();

    // Load saved selections
    loadSavedSelections();

    // Initialize table filtering
    initializeTableFilters();
}

function initializeCheckboxes() {
    // Header checkbox toggles all items in current tab
    $('.tab-pane').each(function() {
        const tabPane = $(this);
        const headerId = tabPane.attr('id');
        const headerCheckbox = tabPane.find('input[type="checkbox"][id^="headerCheck"]');
        const itemCheckboxes = tabPane.find('.item-checkbox');

        if (headerCheckbox.length && itemCheckboxes.length) {
            headerCheckbox.on('change', function() {
                const isChecked = this.checked;
                itemCheckboxes.prop('checked', isChecked);
                updateSelectionCount();
            });

            itemCheckboxes.on('change', function() {
                const totalItems = itemCheckboxes.length;
                const checkedItems = itemCheckboxes.filter(':checked').length;

                headerCheckbox.prop('indeterminate', checkedItems > 0 && checkedItems < totalItems);
                headerCheckbox.prop('checked', checkedItems === totalItems);

                updateSelectionCount();
            });
        }
    });
}

function toggleAllItems(itemType) {
    const checkbox = $(`#selectAll${itemType.charAt(0).toUpperCase() + itemType.slice(1)}`);
    const isChecked = checkbox.is(':checked');

    $(`.item-checkbox[data-type="${itemType}"]`).prop('checked', isChecked);
    updateSelectionCount();
}

function updateSelectionCount() {
    const selectedCount = $('.item-checkbox:checked').length;
    $('.selected-count').text(selectedCount);

    // Update card counter if it exists
    const counterElement = $('.card-body h3:contains("Selected Items")').closest('.card-body').find('h3');
    if (counterElement.length) {
        counterElement.text(selectedCount);
    }
}

function saveSelection() {
    const connectionId = $('#connectionId').val();
    if (!connectionId) {
        showAlert('Please select a connection first.', 'warning');
        return;
    }

    const selectedItems = [];
    $('.item-checkbox:checked').each(function() {
        selectedItems.push({
            id: $(this).data('id'),
            type: $(this).data('type')
        });
    });

    if (selectedItems.length === 0) {
        showAlert('Please select at least one item.', 'warning');
        return;
    }

    const requestData = {
        Type: getCurrentExplorationType(),
        ConnectionId: parseInt(connectionId),
        SelectedItems: selectedItems.map(item => item.id)
    };

    $.ajax({
        url: '/Exploration/SaveSelection',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(requestData),
        success: function(response) {
            if (response.success) {
                showAlert(`Successfully saved ${response.count} selected items.`, 'success');
            } else {
                showAlert('Failed to save selection.', 'error');
            }
        },
        error: function() {
            showAlert('Error saving selection. Please try again.', 'error');
        }
    });
}

function loadSavedSelections() {
    const connectionId = $('#connectionId').val();
    if (!connectionId) return;

    const explorationType = getCurrentExplorationType();

    $.ajax({
        url: '/Exploration/GetSavedSelection',
        method: 'GET',
        data: {
            type: explorationType,
            connectionId: connectionId
        },
        success: function(savedItems) {
            if (savedItems && savedItems.length > 0) {
                savedItems.forEach(function(itemId) {
                    $(`.item-checkbox[data-id="${itemId}"]`).prop('checked', true);
                });
                updateSelectionCount();
            }
        },
        error: function() {
            console.warn('Could not load saved selections');
        }
    });
}

function getCurrentExplorationType() {
    const url = window.location.pathname;
    if (url.includes('/ActiveDirectory')) return 'ActiveDirectory';
    if (url.includes('/EntraId')) return 'EntraId';
    if (url.includes('/Exchange')) return 'Exchange';
    if (url.includes('/ExchangeOnline')) return 'ExchangeOnline';
    return 'Unknown';
}

function performSearch() {
    const searchTerm = $('#searchQuery').val().toLowerCase();

    $('.table tbody tr').each(function() {
        const row = $(this);
        const text = row.text().toLowerCase();

        if (text.includes(searchTerm)) {
            row.show();
        } else {
            row.hide();
        }
    });

    updateVisibleCounts();
}

function updateVisibleCounts() {
    $('.tab-pane').each(function() {
        const tabPane = $(this);
        const visibleRows = tabPane.find('tbody tr:visible').length;
        const totalRows = tabPane.find('tbody tr').length;

        // Update badge in tab
        const tabId = tabPane.attr('id');
        const tab = $(`[data-bs-target="#${tabId}"]`);
        const badge = tab.find('.badge');

        if (badge.length) {
            badge.text(`${visibleRows}/${totalRows}`);
        }
    });
}

function showFilterOptions() {
    $('#filterModal').modal('show');
}

function applyFilters() {
    const filters = {
        mailboxType: $('#mailboxType').val(),
        minSize: $('#minSize').val(),
        maxSize: $('#maxSize').val(),
        showArchiveEnabled: $('#showArchiveEnabled').is(':checked'),
        showSyncedOnly: $('#showSyncedOnly').is(':checked'),
        showCloudOnly: $('#showCloudOnly').is(':checked'),
        showGuestUsers: $('#showGuestUsers').is(':checked'),
        domainFilter: $('#domainFilter').val()
    };

    filterTableRows(filters);
    $('#filterModal').modal('hide');
}

function filterTableRows(filters) {
    $('.table tbody tr').each(function() {
        const row = $(this);
        let shouldShow = true;

        // Mailbox type filter
        if (filters.mailboxType && filters.mailboxType !== 'All') {
            const typeCell = row.find('td').eq(3); // Assuming type is in 4th column
            const typeText = typeCell.text().trim();
            if (!typeText.includes(filters.mailboxType)) {
                shouldShow = false;
            }
        }

        // Size filters (would need actual size data)
        if (filters.minSize || filters.maxSize) {
            const sizeCell = row.find('td').eq(5); // Assuming size is in 6th column
            const sizeText = sizeCell.text().trim();
            // Parse size and apply filters (implementation depends on format)
        }

        // Archive filter
        if (filters.showArchiveEnabled) {
            const archiveCell = row.find('td').eq(8); // Assuming archive is in 9th column
            const hasArchive = archiveCell.text().includes('Yes');
            if (!hasArchive) {
                shouldShow = false;
            }
        }

        // Domain filter
        if (filters.domainFilter) {
            const emailCell = row.find('td').eq(2); // Assuming email is in 3rd column
            const email = emailCell.text().trim();
            if (!email.includes(filters.domainFilter)) {
                shouldShow = false;
            }
        }

        // Guest user filter for Entra ID
        if (filters.showGuestUsers !== undefined) {
            const userTypeCell = row.find('td').eq(6); // Assuming user type is in 7th column
            const isGuest = userTypeCell.text().includes('Guest');
            if (filters.showGuestUsers && !isGuest) {
                shouldShow = false;
            } else if (!filters.showGuestUsers && isGuest) {
                shouldShow = false;
            }
        }

        if (shouldShow) {
            row.show();
        } else {
            row.hide();
        }
    });

    updateVisibleCounts();
}

function initializeTableFilters() {
    // Add search input to each table
    $('.table').each(function() {
        const table = $(this);
        const tableContainer = table.closest('.card-body');

        if (tableContainer.find('.table-search').length === 0) {
            const searchInput = $(`
                <div class="mb-3 table-search">
                    <input type="text" class="form-control form-control-sm" placeholder="Search in table..." style="max-width: 300px;">
                </div>
            `);

            searchInput.insertBefore(table.parent());

            searchInput.find('input').on('input', debounce(function() {
                const searchTerm = $(this).val().toLowerCase();

                table.find('tbody tr').each(function() {
                    const row = $(this);
                    const text = row.text().toLowerCase();

                    if (text.includes(searchTerm)) {
                        row.show();
                    } else {
                        row.hide();
                    }
                });
            }, 200));
        }
    });
}

// Item detail viewing functions
function viewUserDetails(userPrincipalName) {
    const connectionId = $('#connectionId').val();
    if (!connectionId) {
        showAlert('Please select a connection first.', 'warning');
        return;
    }

    $.ajax({
        url: '/Exploration/GetADUserDetails',
        method: 'POST',
        data: {
            userId: userPrincipalName,
            connectionId: connectionId
        },
        success: function(user) {
            showUserDetailsModal(user);
        },
        error: function(xhr) {
            const errorMessage = xhr.responseJSON?.error || 'Failed to load user details.';
            showAlert(errorMessage, 'error');
        }
    });
}

function viewMailboxDetails(emailAddress) {
    $.ajax({
        url: '/Exploration/GetMailboxDetails',
        method: 'POST',
        data: { emailAddress: emailAddress },
        success: function(mailbox) {
            showMailboxDetailsModal(mailbox);
        },
        error: function() {
            showAlert('Failed to load mailbox details.', 'error');
        }
    });
}

function viewGroupDetails(groupId) {
    showAlert('Group details feature coming soon.', 'info');
}

function viewOUDetails(ouId) {
    showAlert('OU details feature coming soon.', 'info');
}

function viewApplicationDetails(appId) {
    showAlert('Application details feature coming soon.', 'info');
}

function viewRoleDetails(roleId) {
    showAlert('Role details feature coming soon.', 'info');
}

function viewSharedMailboxDetails(sharedId) {
    showAlert('Shared mailbox details feature coming soon.', 'info');
}

function showUserDetailsModal(user) {
    const modalHtml = `
        <div class="modal fade" id="userDetailsModal" tabindex="-1">
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">User Details: ${user.displayName}</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <div class="row">
                            <div class="col-md-6">
                                <strong>Display Name:</strong> ${user.displayName || 'N/A'}<br>
                                <strong>UPN:</strong> ${user.userPrincipalName || 'N/A'}<br>
                                <strong>Email:</strong> ${user.emailAddress || 'N/A'}<br>
                                <strong>Department:</strong> ${user.department || 'N/A'}<br>
                                <strong>Title:</strong> ${user.title || 'N/A'}<br>
                            </div>
                            <div class="col-md-6">
                                <strong>Status:</strong> ${user.enabled ? 'Enabled' : 'Disabled'}<br>
                                <strong>Last Logon:</strong> ${user.lastLogon || 'Never'}<br>
                                <strong>Manager:</strong> ${user.manager || 'N/A'}<br>
                                <strong>Groups:</strong> ${user.memberOf ? user.memberOf.join(', ') : 'None'}<br>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                    </div>
                </div>
            </div>
        </div>
    `;

    $('#userDetailsModal').remove();
    $('body').append(modalHtml);
    $('#userDetailsModal').modal('show');
}

function showMailboxDetailsModal(mailbox) {
    const modalHtml = `
        <div class="modal fade" id="mailboxDetailsModal" tabindex="-1">
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Mailbox Details: ${mailbox.displayName}</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <div class="row">
                            <div class="col-md-6">
                                <strong>Display Name:</strong> ${mailbox.displayName || 'N/A'}<br>
                                <strong>Email:</strong> ${mailbox.primarySmtpAddress || 'N/A'}<br>
                                <strong>Alias:</strong> ${mailbox.alias || 'N/A'}<br>
                                <strong>Type:</strong> ${mailbox.mailboxType || 'N/A'}<br>
                                <strong>Database:</strong> ${mailbox.databaseName || 'N/A'}<br>
                            </div>
                            <div class="col-md-6">
                                <strong>Size:</strong> ${formatBytes(mailbox.totalItemSize)}<br>
                                <strong>Item Count:</strong> ${mailbox.itemCount ? mailbox.itemCount.toLocaleString() : 'N/A'}<br>
                                <strong>Archive:</strong> ${mailbox.isArchiveEnabled ? 'Enabled' : 'Disabled'}<br>
                                <strong>Last Logon:</strong> ${mailbox.lastLogonTime || 'Never'}<br>
                                <strong>Server:</strong> ${mailbox.serverName || 'N/A'}<br>
                            </div>
                        </div>
                        ${mailbox.emailAddresses && mailbox.emailAddresses.length > 0 ? `
                        <hr>
                        <strong>Email Addresses:</strong><br>
                        ${mailbox.emailAddresses.map(addr => `<span class="badge bg-secondary me-1">${addr}</span>`).join('')}
                        ` : ''}
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                    </div>
                </div>
            </div>
        </div>
    `;

    $('#mailboxDetailsModal').remove();
    $('body').append(modalHtml);
    $('#mailboxDetailsModal').modal('show');
}

// Utility functions
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

function formatBytes(bytes) {
    if (bytes === 0) return '0 Bytes';

    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));

    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

function showAlert(message, type) {
    const alertClass = type === 'error' ? 'danger' : type;
    const alertHtml = `
        <div class="alert alert-${alertClass} alert-dismissible fade show" role="alert">
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;

    // Remove existing alerts
    $('.alert').remove();

    // Add new alert at the top of main content
    $('main').prepend(alertHtml);

    // Auto-dismiss after 5 seconds
    setTimeout(() => {
        $('.alert').fadeOut();
    }, 5000);
}