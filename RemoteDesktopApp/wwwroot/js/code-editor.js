// Code Editor JavaScript

class CodeEditor {
    constructor() {
        this.currentFile = 'index.html';
        this.files = {
            'index.html': {
                content: '',
                language: 'html',
                modified: false
            },
            'style.css': {
                content: 'body {\n    font-family: \'Vazir\', sans-serif;\n    direction: rtl;\n    text-align: right;\n}',
                language: 'css',
                modified: false
            },
            'script.js': {
                content: 'function showMessage() {\n    alert(\'سلام از JavaScript!\');\n}',
                language: 'javascript',
                modified: false
            }
        };
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.loadCurrentFile();
    }

    setupEventListeners() {
        const textarea = document.getElementById('codeTextarea');
        if (textarea) {
            textarea.addEventListener('input', () => {
                this.markFileAsModified();
                this.updatePreview();
            });

            // Add keyboard shortcuts
            textarea.addEventListener('keydown', (e) => {
                this.handleKeyboardShortcuts(e);
            });
        }
    }

    handleKeyboardShortcuts(e) {
        // Ctrl+S for save
        if (e.ctrlKey && e.key === 's') {
            e.preventDefault();
            this.saveFile();
        }
        
        // Ctrl+R for run
        if (e.ctrlKey && e.key === 'r') {
            e.preventDefault();
            this.runCode();
        }

        // Tab for indentation
        if (e.key === 'Tab') {
            e.preventDefault();
            const textarea = e.target;
            const start = textarea.selectionStart;
            const end = textarea.selectionEnd;
            
            textarea.value = textarea.value.substring(0, start) + '    ' + textarea.value.substring(end);
            textarea.selectionStart = textarea.selectionEnd = start + 4;
        }
    }

    loadCurrentFile() {
        const textarea = document.getElementById('codeTextarea');
        if (textarea && this.files[this.currentFile]) {
            textarea.value = this.files[this.currentFile].content;
        }
    }

    markFileAsModified() {
        if (this.files[this.currentFile]) {
            this.files[this.currentFile].modified = true;
            this.updateFileTab();
        }
    }

    updateFileTab() {
        const tab = document.querySelector(`[data-file="${this.currentFile}"]`);
        if (tab) {
            const span = tab.querySelector('span');
            if (span && this.files[this.currentFile].modified) {
                if (!span.textContent.includes('*')) {
                    span.textContent += ' *';
                }
            }
        }
    }

    updatePreview() {
        const textarea = document.getElementById('codeTextarea');
        const previewFrame = document.getElementById('previewFrame');
        
        if (textarea && previewFrame) {
            const content = textarea.value;
            
            // Update current file content
            if (this.files[this.currentFile]) {
                this.files[this.currentFile].content = content;
            }

            // Update preview for HTML files
            if (this.currentFile.endsWith('.html')) {
                const blob = new Blob([content], { type: 'text/html' });
                const url = URL.createObjectURL(blob);
                previewFrame.src = url;
            }
        }
    }
}

// Global functions for UI interactions
function initializeCodeEditor() {
    window.codeEditor = new CodeEditor();
    console.log('Code editor initialized');
}

function createNewProject() {
    const name = prompt('نام پروژه جدید:');
    if (name) {
        alert(`پروژه "${name}" ایجاد شد!`);
        // Add project creation logic here
    }
}

function createNewFile() {
    const name = prompt('نام فایل جدید (با پسوند):');
    if (name) {
        if (window.codeEditor) {
            window.codeEditor.files[name] = {
                content: '',
                language: getLanguageFromExtension(name),
                modified: false
            };
            openFile(name);
        }
    }
}

function importProject() {
    alert('قابلیت وارد کردن پروژه به زودی اضافه خواهد شد!');
}

function toggleProject(projectId) {
    const projectFiles = document.getElementById(`project-${projectId}`);
    const chevron = document.querySelector(`[onclick="toggleProject(${projectId})"] i.fa-chevron-down, [onclick="toggleProject(${projectId})"] i.fa-chevron-right`);
    
    if (projectFiles && chevron) {
        if (projectFiles.classList.contains('d-none')) {
            projectFiles.classList.remove('d-none');
            chevron.className = 'fas fa-chevron-down ms-auto small';
        } else {
            projectFiles.classList.add('d-none');
            chevron.className = 'fas fa-chevron-right ms-auto small';
        }
    }
}

function openFile(fileName) {
    if (window.codeEditor) {
        window.codeEditor.currentFile = fileName;
        window.codeEditor.loadCurrentFile();
        
        // Update active tab
        document.querySelectorAll('.file-tab').forEach(tab => tab.classList.remove('active'));
        
        // Create or activate tab
        let tab = document.querySelector(`[data-file="${fileName}"]`);
        if (!tab) {
            createFileTab(fileName);
        } else {
            tab.classList.add('active');
        }
        
        window.codeEditor.updatePreview();
    }
}

function createFileTab(fileName) {
    const fileTabs = document.getElementById('fileTabs');
    if (fileTabs) {
        const tab = document.createElement('div');
        tab.className = 'file-tab active d-flex align-items-center px-3 py-1 border rounded-top me-1';
        tab.setAttribute('data-file', fileName);
        
        const icon = getFileIcon(fileName);
        tab.innerHTML = `
            ${icon}
            <span>${fileName}</span>
            <button class="btn btn-sm ms-2 p-0" onclick="closeFile('${fileName}')">
                <i class="fas fa-times small"></i>
            </button>
        `;
        
        // Remove active class from other tabs
        document.querySelectorAll('.file-tab').forEach(t => t.classList.remove('active'));
        
        fileTabs.appendChild(tab);
    }
}

function closeFile(fileName) {
    const tab = document.querySelector(`[data-file="${fileName}"]`);
    if (tab) {
        tab.remove();
        
        // If this was the current file, switch to another tab
        if (window.codeEditor && window.codeEditor.currentFile === fileName) {
            const remainingTabs = document.querySelectorAll('.file-tab');
            if (remainingTabs.length > 0) {
                const nextFile = remainingTabs[0].getAttribute('data-file');
                openFile(nextFile);
            }
        }
    }
}

function saveFile() {
    if (window.codeEditor) {
        const textarea = document.getElementById('codeTextarea');
        if (textarea) {
            window.codeEditor.files[window.codeEditor.currentFile].content = textarea.value;
            window.codeEditor.files[window.codeEditor.currentFile].modified = false;
            
            // Update tab to remove asterisk
            const tab = document.querySelector(`[data-file="${window.codeEditor.currentFile}"]`);
            if (tab) {
                const span = tab.querySelector('span');
                if (span) {
                    span.textContent = span.textContent.replace(' *', '');
                }
            }
            
            addConsoleMessage('فایل ذخیره شد: ' + window.codeEditor.currentFile, 'success');
        }
    }
}

function runCode() {
    if (window.codeEditor) {
        addConsoleMessage('در حال اجرای کد...', 'info');

        const currentFile = window.codeEditor.currentFile;
        const fileContent = window.codeEditor.files[currentFile]?.content || '';
        const language = getLanguageFromExtension(currentFile);

        try {
            if (language === 'javascript') {
                // Execute JavaScript code
                executeJavaScript(fileContent);
            } else if (language === 'html') {
                // Update preview for HTML
                window.codeEditor.updatePreview();
                addConsoleMessage('HTML به‌روزرسانی شد!', 'success');
            } else {
                // For other languages, just update preview
                window.codeEditor.updatePreview();
                addConsoleMessage('کد با موفقیت اجرا شد!', 'success');
            }
        } catch (error) {
            addConsoleMessage('خطا در اجرای کد: ' + error.message, 'error');
        }
    }
}

function executeJavaScript(code) {
    try {
        // Create a safe execution context
        const originalConsoleLog = console.log;
        const originalAlert = window.alert;

        // Override console.log to show in our console
        console.log = function(...args) {
            addConsoleMessage('خروجی: ' + args.join(' '), 'info');
            originalConsoleLog.apply(console, args);
        };

        // Override alert to show in our console
        window.alert = function(message) {
            addConsoleMessage('هشدار: ' + message, 'info');
            originalAlert(message);
        };

        // Execute the code
        const result = eval(code);

        if (result !== undefined) {
            addConsoleMessage('نتیجه: ' + result, 'success');
        } else {
            addConsoleMessage('کد JavaScript با موفقیت اجرا شد!', 'success');
        }

        // Restore original functions
        console.log = originalConsoleLog;
        window.alert = originalAlert;

    } catch (error) {
        addConsoleMessage('خطای JavaScript: ' + error.message, 'error');
        console.error('JavaScript execution error:', error);
    }
}

function getLanguageFromExtension(filename) {
    const extension = filename.split('.').pop().toLowerCase();
    const languageMap = {
        'js': 'javascript',
        'html': 'html',
        'css': 'css',
        'py': 'python',
        'java': 'java',
        'cpp': 'cpp',
        'c': 'c',
        'cs': 'csharp',
        'php': 'php',
        'rb': 'ruby',
        'go': 'go',
        'rs': 'rust',
        'ts': 'typescript',
        'json': 'json',
        'xml': 'xml',
        'md': 'markdown'
    };
    return languageMap[extension] || 'text';
}

function formatCode() {
    const textarea = document.getElementById('codeTextarea');
    if (textarea) {
        // Simple formatting for demonstration
        let content = textarea.value;
        
        // Basic HTML formatting
        if (window.codeEditor.currentFile.endsWith('.html')) {
            content = content.replace(/></g, '>\n<');
            content = content.replace(/\n\s*\n/g, '\n');
        }
        
        textarea.value = content;
        addConsoleMessage('کد قالب‌بندی شد!', 'success');
    }
}

function refreshPreview() {
    if (window.codeEditor) {
        window.codeEditor.updatePreview();
        addConsoleMessage('پیش‌نمایش به‌روزرسانی شد', 'info');
    }
}

function togglePreview() {
    const previewPanel = document.getElementById('previewPanel');
    if (previewPanel) {
        if (previewPanel.style.display === 'none') {
            previewPanel.style.display = 'block';
        } else {
            previewPanel.style.display = 'none';
        }
    }
}

function showConsoleTab(tabName) {
    // Update active button
    document.querySelectorAll('[onclick^="showConsoleTab"]').forEach(btn => {
        btn.classList.remove('active');
    });
    document.querySelector(`[onclick="showConsoleTab('${tabName}')"]`).classList.add('active');
    
    // Show appropriate content
    const consoleContent = document.getElementById('consoleContent');
    if (consoleContent) {
        switch (tabName) {
            case 'console':
                // Keep existing console content
                break;
            case 'output':
                consoleContent.innerHTML = '<div class="text-info">خروجی برنامه اینجا نمایش داده می‌شود...</div>';
                break;
            case 'problems':
                consoleContent.innerHTML = '<div class="text-warning">هیچ مشکلی یافت نشد!</div>';
                break;
        }
    }
}

function clearConsole() {
    const consoleContent = document.getElementById('consoleContent');
    if (consoleContent) {
        consoleContent.innerHTML = '<div class="text-success">کنسول پاک شد...</div>';
    }
}

function addConsoleMessage(message, type = 'info') {
    const consoleContent = document.getElementById('consoleContent');
    if (consoleContent) {
        const timestamp = new Date().toLocaleTimeString('fa-IR');
        const colorClass = type === 'success' ? 'text-success' : 
                          type === 'error' ? 'text-danger' : 
                          type === 'warning' ? 'text-warning' : 'text-info';
        
        const messageDiv = document.createElement('div');
        messageDiv.className = colorClass;
        messageDiv.innerHTML = `[${timestamp}] ${message}`;
        
        consoleContent.appendChild(messageDiv);
        consoleContent.scrollTop = consoleContent.scrollHeight;
    }
}

function showSettings() {
    alert('تنظیمات ویرایشگر به زودی اضافه خواهد شد!');
}

function toggleTheme() {
    alert('تغییر تم به زودی اضافه خواهد شد!');
}

function showKeyboardShortcuts() {
    alert(`میانبرهای کیبورد:
    
Ctrl+S: ذخیره فایل
Ctrl+R: اجرای کد
Tab: تورفتگی
Ctrl+Z: بازگشت
Ctrl+Y: تکرار`);
}

// Helper functions
function getLanguageFromExtension(fileName) {
    const ext = fileName.split('.').pop().toLowerCase();
    const languageMap = {
        'html': 'html',
        'css': 'css',
        'js': 'javascript',
        'jsx': 'javascript',
        'ts': 'typescript',
        'tsx': 'typescript',
        'json': 'json',
        'xml': 'xml',
        'md': 'markdown'
    };
    return languageMap[ext] || 'text';
}

function getFileIcon(fileName) {
    const ext = fileName.split('.').pop().toLowerCase();
    const iconMap = {
        'html': '<i class="fab fa-html5 text-danger me-2"></i>',
        'css': '<i class="fab fa-css3-alt text-primary me-2"></i>',
        'js': '<i class="fab fa-js-square text-warning me-2"></i>',
        'jsx': '<i class="fab fa-react text-info me-2"></i>',
        'ts': '<i class="fas fa-file-code text-primary me-2"></i>',
        'tsx': '<i class="fab fa-react text-info me-2"></i>',
        'json': '<i class="fas fa-file-code text-success me-2"></i>',
        'md': '<i class="fab fa-markdown text-dark me-2"></i>'
    };
    return iconMap[ext] || '<i class="fas fa-file text-secondary me-2"></i>';
}
