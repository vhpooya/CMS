// Unit Management JavaScript

let currentUnit = null;
let allUnits = [];
let allUsers = [];

// Initialize unit management
$(document).ready(function() {
    loadUnitsData();
    loadUsersData();
    setupEventHandlers();
});

// Setup event handlers
function setupEventHandlers() {
    // Search on Enter key
    $('#searchUnits').on('keypress', function(e) {
        if (e.which === 13) {
            searchUnits();
        }
    });

    // Auto-generate unit code from name
    $('#unitName').on('input', function() {
        const name = $(this).val();
        const code = name.replace(/\s+/g, '_').toUpperCase().substring(0, 10);
        $('#unitCode').val(code);
    });
}

// Load units data
async function loadUnitsData() {
    try {
        const response = await fetch('/api/unit', {
            credentials: 'include'
        });

        if (response.ok) {
            allUnits = await response.json();
            displayUnitsHierarchy();
            updateStatistics();
            populateParentUnitDropdown();
        } else {
            console.error('Failed to load units');
            showMockUnits();
        }
    } catch (error) {
        console.error('Error loading units:', error);
        showMockUnits();
    }
}

// Load users data
async function loadUsersData() {
    try {
        const response = await fetch('/api/dashboard/users', {
            credentials: 'include'
        });

        if (response.ok) {
            allUsers = await response.json();
            populateManagerDropdown();
        } else {
            console.error('Failed to load users');
        }
    } catch (error) {
        console.error('Error loading users:', error);
    }
}

// Display units hierarchy
function displayUnitsHierarchy() {
    const container = document.getElementById('unitsTree');
    container.innerHTML = '';

    const rootUnits = allUnits.filter(u => !u.parentUnitId);
    
    if (rootUnits.length === 0) {
        container.innerHTML = `
            <div class="text-center py-4">
                <i class="fas fa-building fa-3x text-muted mb-3"></i>
                <p class="text-muted">هیچ واحدی تعریف نشده است</p>
                <button class="btn btn-primary" onclick="showCreateUnitModal()">
                    <i class="fas fa-plus me-2"></i>
                    ایجاد اولین واحد
                </button>
            </div>
        `;
        return;
    }

    rootUnits.forEach(unit => {
        const unitElement = createUnitTreeNode(unit);
        container.appendChild(unitElement);
    });
}

// Create unit tree node
function createUnitTreeNode(unit) {
    const subUnits = allUnits.filter(u => u.parentUnitId === unit.id);
    const userCount = unit.users ? unit.users.length : 0;
    
    const nodeDiv = document.createElement('div');
    nodeDiv.className = 'tree-node';
    nodeDiv.innerHTML = `
        <div class="tree-item" onclick="selectUnit(${unit.id})">
            <div class="d-flex justify-content-between align-items-center">
                <div class="d-flex align-items-center">
                    ${subUnits.length > 0 ? 
                        `<i class="fas fa-chevron-down tree-toggle me-2" onclick="toggleTreeNode(event, this)"></i>` : 
                        `<span class="tree-spacer me-2"></span>`
                    }
                    <i class="fas fa-building text-primary me-2"></i>
                    <strong>${unit.name}</strong>
                    <span class="badge bg-secondary ms-2">${unit.code}</span>
                </div>
                <div>
                    <span class="badge bg-info me-2">${userCount} کاربر</span>
                    <button class="btn btn-sm btn-outline-primary" onclick="showUnitDetails(${unit.id})" title="جزئیات">
                        <i class="fas fa-eye"></i>
                    </button>
                </div>
            </div>
            ${unit.description ? `<small class="text-muted d-block mt-1">${unit.description}</small>` : ''}
        </div>
    `;

    if (subUnits.length > 0) {
        const childrenDiv = document.createElement('div');
        childrenDiv.className = 'tree-children ms-4';
        
        subUnits.forEach(subUnit => {
            const childNode = createUnitTreeNode(subUnit);
            childrenDiv.appendChild(childNode);
        });
        
        nodeDiv.appendChild(childrenDiv);
    }

    return nodeDiv;
}

// Toggle tree node
function toggleTreeNode(event, element) {
    event.stopPropagation();
    
    const treeNode = element.closest('.tree-node');
    const children = treeNode.querySelector('.tree-children');
    
    if (children) {
        if (children.style.display === 'none') {
            children.style.display = 'block';
            element.className = 'fas fa-chevron-down tree-toggle me-2';
        } else {
            children.style.display = 'none';
            element.className = 'fas fa-chevron-right tree-toggle me-2';
        }
    }
}

// Select unit
function selectUnit(unitId) {
    const unit = allUnits.find(u => u.id === unitId);
    if (unit) {
        currentUnit = unit;
        // Highlight selected unit
        document.querySelectorAll('.tree-item').forEach(item => {
            item.classList.remove('selected');
        });
        event.target.closest('.tree-item').classList.add('selected');
    }
}

// Show units list view
function showUnitsList() {
    document.getElementById('unitsHierarchyView').style.display = 'none';
    document.getElementById('unitsListView').style.display = 'block';
    
    // Update button states
    document.querySelectorAll('.btn-group .btn').forEach(btn => btn.classList.remove('active'));
    event.target.classList.add('active');
    
    displayUnitsList();
}

// Show units hierarchy view
function showUnitsHierarchy() {
    document.getElementById('unitsHierarchyView').style.display = 'block';
    document.getElementById('unitsListView').style.display = 'none';
    
    // Update button states
    document.querySelectorAll('.btn-group .btn').forEach(btn => btn.classList.remove('active'));
    event.target.classList.add('active');
}

// Display units list
function displayUnitsList() {
    const tbody = document.querySelector('#unitsTable tbody');
    tbody.innerHTML = '';

    allUnits.forEach(unit => {
        const parentUnit = unit.parentUnitId ? allUnits.find(u => u.id === unit.parentUnitId) : null;
        const manager = unit.managerId ? allUsers.find(u => u.id === unit.managerId) : null;
        const userCount = unit.users ? unit.users.length : 0;

        const row = document.createElement('tr');
        row.innerHTML = `
            <td>
                <div class="d-flex align-items-center">
                    <i class="fas fa-building text-primary me-2"></i>
                    <strong>${unit.name}</strong>
                </div>
                ${unit.description ? `<small class="text-muted">${unit.description}</small>` : ''}
            </td>
            <td><span class="badge bg-secondary">${unit.code}</span></td>
            <td>${parentUnit ? parentUnit.name : '-'}</td>
            <td>${manager ? manager.displayName : '-'}</td>
            <td><span class="badge bg-info">${userCount}</span></td>
            <td><span class="badge bg-success">فعال</span></td>
            <td>
                <div class="btn-group btn-group-sm">
                    <button class="btn btn-outline-primary" onclick="showUnitDetails(${unit.id})" title="جزئیات">
                        <i class="fas fa-eye"></i>
                    </button>
                    <button class="btn btn-outline-warning" onclick="editUnit(${unit.id})" title="ویرایش">
                        <i class="fas fa-edit"></i>
                    </button>
                    <button class="btn btn-outline-danger" onclick="deleteUnit(${unit.id})" title="حذف">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            </td>
        `;
        tbody.appendChild(row);
    });
}

// Update statistics
function updateStatistics() {
    const totalUnits = allUnits.length;
    const totalUsers = allUnits.reduce((sum, unit) => sum + (unit.users ? unit.users.length : 0), 0);
    const unassignedUsers = allUsers.filter(u => !u.unitId).length;

    document.getElementById('totalUnitsCount').textContent = totalUnits;
    document.getElementById('totalUsersCount').textContent = totalUsers;
    document.getElementById('unassignedUsersCount').textContent = unassignedUsers;
    document.getElementById('activeLinksCount').textContent = '0'; // Will be updated when links are loaded
}

// Show create unit modal
function showCreateUnitModal() {
    document.getElementById('createUnitForm').reset();
    populateParentUnitDropdown();
    populateManagerDropdown();
    
    const modal = new bootstrap.Modal(document.getElementById('createUnitModal'));
    modal.show();
}

// Populate parent unit dropdown
function populateParentUnitDropdown() {
    const select = document.getElementById('parentUnit');
    select.innerHTML = '<option value="">انتخاب کنید...</option>';

    allUnits.forEach(unit => {
        const option = document.createElement('option');
        option.value = unit.id;
        option.textContent = unit.name;
        select.appendChild(option);
    });
}

// Populate manager dropdown
function populateManagerDropdown() {
    const select = document.getElementById('unitManager');
    select.innerHTML = '<option value="">انتخاب کنید...</option>';

    allUsers.forEach(user => {
        const option = document.createElement('option');
        option.value = user.id;
        option.textContent = user.displayName;
        select.appendChild(option);
    });
}

// Create unit
async function createUnit() {
    const formData = {
        name: document.getElementById('unitName').value,
        code: document.getElementById('unitCode').value,
        description: document.getElementById('unitDescription').value,
        parentUnitId: document.getElementById('parentUnit').value || null,
        managerId: document.getElementById('unitManager').value || null,
        location: document.getElementById('unitLocation').value,
        phoneExtension: document.getElementById('unitPhone').value,
        email: document.getElementById('unitEmail').value
    };

    try {
        const response = await fetch('/api/unit', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include',
            body: JSON.stringify(formData)
        });

        if (response.ok) {
            const newUnit = await response.json();
            allUnits.push(newUnit);
            displayUnitsHierarchy();
            updateStatistics();
            
            // Close modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('createUnitModal'));
            modal.hide();
            
            showSuccessMessage('واحد با موفقیت ایجاد شد');
        } else {
            const error = await response.json();
            showErrorMessage(error.message || 'خطا در ایجاد واحد');
        }
    } catch (error) {
        console.error('Error creating unit:', error);
        showErrorMessage('خطا در ایجاد واحد');
    }
}

// Search units
function searchUnits() {
    const searchTerm = document.getElementById('searchUnits').value.trim();
    
    if (!searchTerm) {
        displayUnitsHierarchy();
        return;
    }

    const filteredUnits = allUnits.filter(unit => 
        unit.name.includes(searchTerm) || 
        unit.code.includes(searchTerm) || 
        unit.description.includes(searchTerm)
    );

    // Display filtered results
    const container = document.getElementById('unitsTree');
    container.innerHTML = '';

    if (filteredUnits.length === 0) {
        container.innerHTML = `
            <div class="text-center py-4">
                <i class="fas fa-search fa-3x text-muted mb-3"></i>
                <p class="text-muted">هیچ واحدی یافت نشد</p>
            </div>
        `;
        return;
    }

    filteredUnits.forEach(unit => {
        const unitElement = createUnitTreeNode(unit);
        container.appendChild(unitElement);
    });
}

// Show mock units for demo
function showMockUnits() {
    allUnits = [
        {
            id: 1,
            name: 'مدیریت عامل',
            code: 'MGT',
            description: 'مدیریت عامل شرکت',
            parentUnitId: null,
            managerId: 1,
            users: [{ id: 1, displayName: 'مدیر عامل' }]
        },
        {
            id: 2,
            name: 'واحد فناوری اطلاعات',
            code: 'IT',
            description: 'واحد فناوری اطلاعات و ارتباطات',
            parentUnitId: 1,
            managerId: 2,
            users: [
                { id: 2, displayName: 'مدیر IT' },
                { id: 3, displayName: 'برنامه‌نویس' }
            ]
        },
        {
            id: 3,
            name: 'واحد مالی',
            code: 'FIN',
            description: 'واحد مالی و حسابداری',
            parentUnitId: 1,
            managerId: 4,
            users: [
                { id: 4, displayName: 'مدیر مالی' },
                { id: 5, displayName: 'حسابدار' }
            ]
        }
    ];

    displayUnitsHierarchy();
    updateStatistics();
    populateParentUnitDropdown();
}

// Utility functions
function showSuccessMessage(message) {
    // Implementation for success message
    alert(message);
}

function showErrorMessage(message) {
    // Implementation for error message
    alert(message);
}

// Placeholder functions for future implementation
function showUnitDetails(unitId) {
    console.log('Show unit details:', unitId);
}

function editUnit(unitId) {
    console.log('Edit unit:', unitId);
}

function deleteUnit(unitId) {
    console.log('Delete unit:', unitId);
}
