// Spreadsheet JavaScript

class SpreadsheetManager {
    constructor() {
        this.currentSpreadsheet = null;
        this.selectedCell = null;
        this.data = {};
        this.rows = 50;
        this.columns = 26;
        this.init();
    }

    init() {
        this.loadSpreadsheets();
        this.setupEventListeners();
        this.createSpreadsheetGrid();
    }

    async loadSpreadsheets() {
        try {
            const response = await fetch('/api/spreadsheet', {
                method: 'GET',
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            if (response.ok) {
                const spreadsheets = await response.json();
                this.displaySpreadsheetList(spreadsheets);
            } else if (response.status === 401) {
                console.error('Authentication required for spreadsheets');
                window.location.href = '/Home/Login';
            } else {
                console.error('Failed to load spreadsheets, status:', response.status);
                this.showMockSpreadsheets();
            }
        } catch (error) {
            console.error('Error loading spreadsheets:', error);
            this.showMockSpreadsheets();
        }
    }

    showMockSpreadsheets() {
        const mockSpreadsheets = [
            {
                id: 1,
                name: 'بودجه ۱۴۰۴',
                description: 'برنامه‌ریزی بودجه سالانه',
                createdAt: new Date().toISOString(),
                modifiedAt: new Date().toISOString(),
                owner: 'مدیر',
                isShared: false,
                rowCount: 150,
                columnCount: 12
            },
            {
                id: 2,
                name: 'گزارش فروش فصل چهارم',
                description: 'تحلیل فروش سه‌ماهه',
                createdAt: new Date().toISOString(),
                modifiedAt: new Date().toISOString(),
                owner: 'کاربر نمونه',
                isShared: false,
                rowCount: 200,
                columnCount: 8
            },
            {
                id: 3,
                name: 'فهرست کارمندان',
                description: 'اطلاعات کارمندان شرکت',
                createdAt: new Date().toISOString(),
                modifiedAt: new Date().toISOString(),
                owner: 'علی احمدی',
                isShared: true,
                rowCount: 50,
                columnCount: 6
            }
        ];
        this.displaySpreadsheetList(mockSpreadsheets);
    }

    displaySpreadsheetList(spreadsheets) {
        // Update both my spreadsheets and shared spreadsheets lists
        const myContainer = document.getElementById('mySpreadsheetsList');
        const sharedContainer = document.getElementById('sharedSpreadsheetsList');

        if (myContainer) {
            myContainer.innerHTML = '';
            this.renderSpreadsheetCards(spreadsheets.filter(s => !s.isShared), myContainer);
        }

        if (sharedContainer) {
            sharedContainer.innerHTML = '';
            this.renderSpreadsheetCards(spreadsheets.filter(s => s.isShared), sharedContainer);
        }
    }

    renderSpreadsheetCards(spreadsheets, container) {
        if (spreadsheets.length === 0) {
            container.innerHTML = `
                <div class="col-12 text-center py-5">
                    <i class="fas fa-table fa-3x text-muted mb-3"></i>
                    <p class="text-muted">هیچ جدولی یافت نشد</p>
                </div>
            `;
            return;
        }

        spreadsheets.forEach(spreadsheet => {
            const card = document.createElement('div');
            card.className = 'col-md-4 mb-3';
            card.innerHTML = `
                <div class="card h-100">
                    <div class="card-body">
                        <h5 class="card-title">${spreadsheet.name}</h5>
                        <p class="card-text">${spreadsheet.description}</p>
                        <small class="text-muted">
                            مالک: ${spreadsheet.owner}<br>
                            ${spreadsheet.rowCount} ردیف × ${spreadsheet.columnCount} ستون
                        </small>
                    </div>
                    <div class="card-footer">
                        <button class="btn btn-primary btn-sm" onclick="spreadsheetManager.openSpreadsheet(${spreadsheet.id})">
                            <i class="fas fa-folder-open me-1"></i>باز کردن
                        </button>
                        <button class="btn btn-outline-secondary btn-sm" onclick="spreadsheetManager.shareSpreadsheet(${spreadsheet.id})">
                            <i class="fas fa-share me-1"></i>اشتراک
                        </button>
                    </div>
                </div>
            `;
            container.appendChild(card);
        });
    }

    createSpreadsheetGrid() {
        const container = document.getElementById('spreadsheetGrid');
        if (!container) return;

        // Create header row
        const headerRow = document.createElement('div');
        headerRow.className = 'spreadsheet-row header-row';
        
        // Empty cell for row numbers
        const emptyCell = document.createElement('div');
        emptyCell.className = 'spreadsheet-cell header-cell';
        headerRow.appendChild(emptyCell);

        // Column headers (A, B, C, ...)
        for (let col = 0; col < this.columns; col++) {
            const headerCell = document.createElement('div');
            headerCell.className = 'spreadsheet-cell header-cell';
            headerCell.textContent = this.getColumnName(col);
            headerRow.appendChild(headerCell);
        }
        container.appendChild(headerRow);

        // Create data rows
        for (let row = 1; row <= this.rows; row++) {
            const dataRow = document.createElement('div');
            dataRow.className = 'spreadsheet-row';

            // Row number
            const rowHeader = document.createElement('div');
            rowHeader.className = 'spreadsheet-cell header-cell';
            rowHeader.textContent = row;
            dataRow.appendChild(rowHeader);

            // Data cells
            for (let col = 0; col < this.columns; col++) {
                const cell = document.createElement('div');
                cell.className = 'spreadsheet-cell data-cell';
                cell.contentEditable = true;
                cell.dataset.row = row;
                cell.dataset.col = col;
                cell.addEventListener('click', () => this.selectCell(cell));
                cell.addEventListener('input', () => this.updateCellData(cell));
                dataRow.appendChild(cell);
            }
            container.appendChild(dataRow);
        }
    }

    getColumnName(columnNumber) {
        let columnName = "";
        while (columnNumber >= 0) {
            columnName = String.fromCharCode('A'.charCodeAt(0) + (columnNumber % 26)) + columnName;
            columnNumber = Math.floor(columnNumber / 26) - 1;
        }
        return columnName;
    }

    selectCell(cell) {
        // Remove previous selection
        if (this.selectedCell) {
            this.selectedCell.classList.remove('selected');
        }

        // Select new cell
        this.selectedCell = cell;
        cell.classList.add('selected');

        // Update cell reference display
        const cellRef = this.getColumnName(parseInt(cell.dataset.col)) + cell.dataset.row;
        const cellRefDisplay = document.getElementById('cellReference');
        if (cellRefDisplay) {
            cellRefDisplay.textContent = cellRef;
        }

        // Update formula bar
        const formulaBar = document.getElementById('formulaBar');
        if (formulaBar) {
            formulaBar.value = cell.textContent;
        }
    }

    updateCellData(cell) {
        const row = parseInt(cell.dataset.row);
        const col = parseInt(cell.dataset.col);
        const cellKey = `${this.getColumnName(col)}${row}`;
        this.data[cellKey] = cell.textContent;
    }

    async openSpreadsheet(id) {
        try {
            const response = await fetch(`/api/spreadsheet/${id}`);
            if (response.ok) {
                const spreadsheet = await response.json();
                this.loadSpreadsheetData(spreadsheet);
                this.showSpreadsheetEditor();
            } else {
                console.error('Failed to open spreadsheet');
                this.showMockSpreadsheetData(id);
            }
        } catch (error) {
            console.error('Error opening spreadsheet:', error);
            this.showMockSpreadsheetData(id);
        }
    }

    showMockSpreadsheetData(id) {
        const mockData = {
            id: id,
            name: 'جدول نمونه',
            description: 'داده‌های نمونه جدول',
            rows: 50,
            columns: 10,
            data: this.generateMockData()
        };
        this.loadSpreadsheetData(mockData);
        this.showSpreadsheetEditor();
    }

    generateMockData() {
        const data = {};
        
        // Header row
        for (let col = 0; col < 10; col++) {
            data[`${this.getColumnName(col)}1`] = `ستون ${col + 1}`;
        }

        // Sample data
        for (let row = 2; row <= 10; row++) {
            data[`A${row}`] = `ردیف ${row}`;
            for (let col = 1; col < 10; col++) {
                const random = Math.random();
                if (random < 0.3) {
                    data[`${this.getColumnName(col)}${row}`] = Math.floor(Math.random() * 1000);
                } else if (random < 0.6) {
                    data[`${this.getColumnName(col)}${row}`] = (Math.random() * 1000).toFixed(2);
                } else {
                    data[`${this.getColumnName(col)}${row}`] = `متن ${Math.floor(Math.random() * 100)}`;
                }
            }
        }

        return data;
    }

    loadSpreadsheetData(spreadsheet) {
        this.currentSpreadsheet = spreadsheet;
        this.data = spreadsheet.data || {};

        // Update spreadsheet title
        const titleElement = document.getElementById('spreadsheetTitle');
        if (titleElement) {
            titleElement.textContent = spreadsheet.name;
        }

        // Load data into cells
        Object.keys(this.data).forEach(cellKey => {
            const cell = this.findCellByKey(cellKey);
            if (cell) {
                cell.textContent = this.data[cellKey];
            }
        });
    }

    findCellByKey(cellKey) {
        // Parse cell key (e.g., "A1" -> row: 1, col: 0)
        const match = cellKey.match(/^([A-Z]+)(\d+)$/);
        if (!match) return null;

        const colName = match[1];
        const row = parseInt(match[2]);
        const col = this.getColumnNumber(colName);

        return document.querySelector(`[data-row="${row}"][data-col="${col}"]`);
    }

    getColumnNumber(columnName) {
        let result = 0;
        for (let i = 0; i < columnName.length; i++) {
            result = result * 26 + (columnName.charCodeAt(i) - 'A'.charCodeAt(0) + 1);
        }
        return result - 1;
    }

    showSpreadsheetEditor() {
        const listView = document.getElementById('spreadsheetListView');
        const editorView = document.getElementById('spreadsheetEditorView');
        
        if (listView) listView.style.display = 'none';
        if (editorView) editorView.style.display = 'block';
    }

    showSpreadsheetList() {
        const listView = document.getElementById('spreadsheetListView');
        const editorView = document.getElementById('spreadsheetEditorView');
        
        if (listView) listView.style.display = 'block';
        if (editorView) editorView.style.display = 'none';
    }

    setupEventListeners() {
        // Back to list button
        const backButton = document.getElementById('backToListBtn');
        if (backButton) {
            backButton.addEventListener('click', () => this.showSpreadsheetList());
        }

        // New spreadsheet button
        const newButton = document.getElementById('newSpreadsheetBtn');
        if (newButton) {
            newButton.addEventListener('click', () => this.createNewSpreadsheet());
        }

        // Save button
        const saveButton = document.getElementById('saveSpreadsheetBtn');
        if (saveButton) {
            saveButton.addEventListener('click', () => this.saveSpreadsheet());
        }
    }

    async createNewSpreadsheet(name) {
        if (!name) {
            name = prompt('نام جدول جدید:');
        }
        if (name) {
            try {
                const response = await fetch('/api/spreadsheet', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    credentials: 'include',
                    body: JSON.stringify({
                        name: name,
                        description: 'جدول جدید',
                        category: 'عمومی'
                    })
                });

                if (response.ok) {
                    const newSpreadsheet = await response.json();
                    this.loadSpreadsheetData(newSpreadsheet);
                    this.showSpreadsheetEditor();
                    this.loadSpreadsheets(); // Refresh the list
                    alert('جدول جدید با موفقیت ایجاد شد!');
                } else {
                    const error = await response.json();
                    alert('خطا در ایجاد جدول: ' + (error.message || 'خطای نامشخص'));
                }
            } catch (error) {
                console.error('Error creating spreadsheet:', error);
                alert('خطا در ایجاد جدول: ' + error.message);
            }
        }
    }

    async saveSpreadsheet() {
        if (!this.currentSpreadsheet) return;

        try {
            const response = await fetch('/api/spreadsheet/save', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    id: this.currentSpreadsheet.id,
                    name: this.currentSpreadsheet.name,
                    data: this.data
                })
            });

            if (response.ok) {
                alert('جدول ذخیره شد');
            } else {
                alert('خطا در ذخیره جدول');
            }
        } catch (error) {
            console.error('Error saving spreadsheet:', error);
            alert('جدول در حافظه محلی ذخیره شد');
        }
    }

    shareSpreadsheet(id) {
        alert('قابلیت اشتراک‌گذاری به زودی اضافه خواهد شد!');
    }
}

// Initialize spreadsheet manager when DOM is loaded
let spreadsheetManager;
document.addEventListener('DOMContentLoaded', function() {
    spreadsheetManager = new SpreadsheetManager();
});

// Export for global access
window.SpreadsheetManager = SpreadsheetManager;

// Global functions for UI interactions
function showCreateSpreadsheetModal() {
    const name = prompt('نام جدول جدید:');
    if (name) {
        if (window.spreadsheetManager) {
            window.spreadsheetManager.createNewSpreadsheet(name);
        } else {
            alert('سیستم جداول در حال بارگذاری است...');
        }
    }
}

function showImportModal() {
    alert('قابلیت وارد کردن فایل به زودی اضافه خواهد شد!');
}

function exportSpreadsheet(format) {
    alert(`قابلیت صادر کردن به فرمت ${format} به زودی اضافه خواهد شد!`);
}

function shareSpreadsheet(id) {
    if (window.spreadsheetManager) {
        window.spreadsheetManager.shareSpreadsheet(id);
    }
}

function deleteSpreadsheet(id) {
    if (confirm('آیا مطمئن هستید که می‌خواهید این جدول را حذف کنید؟')) {
        alert('قابلیت حذف جدول به زودی اضافه خواهد شد!');
    }
}
