// Initialize Terminal
const term = new Terminal({
    cursorBlink: true,
    fontFamily: '"Consolas", "VT323", monospace',
    fontSize: 18,
    theme: {
        background: '#000000', // Classic Borland blue
        foreground: '#FFFFFF', // White text
        cursor: '#FFFFFF',     // White cursor
        // Add more color customizations for terminal content
        brightBlue: '#5555FF', // Brighter blue for highlights
        brightRed: '#FF5555',  // Bright red for errors/accents
        brightYellow: '#FFFF55' // Bright yellow for warnings/highlights
        
    },
    allowTransparency: false,
    scrollback: 1000,
    tabStopWidth: 8
});

// Load addons (using xterm.js v3.14.5 syntax)
Terminal.rows = 24;
Terminal.applyAddon(fit);
Terminal.applyAddon(webLinks);

// Initialize connected state
let isConnected = false;
let lastKnownCols = 0;
let lastKnownRows = 0;

// Function to validate tab ID exists or create a fallback
function validateTabId() {
    if (typeof window.tabId === 'undefined' || !window.tabId) {
        console.error("No tab ID assigned to this terminal!");
        window.tabId = "unknown-" + Date.now(); // Fallback ID
        return false;
    }
    return true;
}

// Debounce function to limit resize events
function debounce(func, wait) {
    let timeout;
    return function(...args) {
        const context = this;
        clearTimeout(timeout);
        timeout = setTimeout(() => func.apply(context, args), wait);
    };
}

// Function to handle terminal resizing
function fitTerminal() {
    try {
        if (term) {
            // Log container size before fitting for debugging
            const container = document.getElementById('terminal-container');
            console.log(`Container size before fit: ${container.clientWidth}x${container.clientHeight}`);
            
            term.fit();
            
            // Log terminal size after fitting
            console.log(`Terminal size after fit: ${term.cols}x${term.rows}`);
            
            // Check if dimensions actually changed to avoid unnecessary resize messages
            if (term.cols !== lastKnownCols || term.rows !== lastKnownRows) {
                lastKnownCols = term.cols;
                lastKnownRows = term.rows;
                
                // Report terminal size back to C# for SSH terminal resize
                reportTerminalSize();
            }
        }
    } catch (err) {
        console.error('Error when fitting terminal:', err);
    }
}

// Report terminal size back to C# for SSH terminal resize
function reportTerminalSize() {
    if (window.chrome && window.chrome.webview) {
        validateTabId();
        
        if (!term.cols || !term.rows) {
            console.error('Terminal dimensions not available');
            return;
        }
        
        const dimensions = {
            cols: term.cols,
            rows: term.rows  // Don't subtract 2 rows anymore
        };
        
        window.chrome.webview.postMessage(JSON.stringify({
            type: 'resize',
            tabId: window.tabId,
            dimensions: dimensions
        }));
        
        console.log(`Reported terminal size: ${term.cols}x${term.rows} for tab ${window.tabId}`);
    }
}

// Apply a complete theme from C#
function applyTerminalTheme(themeColors) {
    console.log(`Tab ${window.tabId}: Applying terminal theme:`, themeColors);

    if (!themeColors) return;

    // Create a new theme object for xterm.js
    const xtermTheme = {
        background: themeColors.background || '#0000AA',
        foreground: themeColors.foreground || '#FFFFFF',
        cursor: themeColors.cursor || '#FFFFFF',
        black: themeColors.black || '#000000',
        red: themeColors.red || '#AA0000',
        green: themeColors.green || '#00AA00',
        yellow: themeColors.yellow || '#AA5500',
        blue: themeColors.blue || '#0000AA',
        magenta: themeColors.magenta || '#AA00AA',
        cyan: themeColors.cyan || '#00AAAA',
        white: themeColors.white || '#AAAAAA',
        brightBlack: themeColors.brightBlack || '#555555',
        brightRed: themeColors.brightRed || '#FF5555',
        brightGreen: themeColors.brightGreen || '#55FF55',
        brightYellow: themeColors.brightYellow || '#FFFF55',
        brightBlue: themeColors.brightBlue || '#5555FF',
        brightMagenta: themeColors.brightMagenta || '#FF55FF',
        brightCyan: themeColors.brightCyan || '#55FFFF',
        brightWhite: themeColors.brightWhite || '#FFFFFF'
    };

    // Apply the theme to xterm.js
    term.setOption('theme', xtermTheme);
    
    // Apply scrollbar colors if provided
    if (themeColors.scrollbarBackground || themeColors.scrollbarThumb) {
        applyScrollbarColors(
            themeColors.scrollbarBackground, 
            themeColors.scrollbarThumb
        );
    }
    
    // Apply changes and resize
    fitTerminal();
}

// Updated function to keep the checkered pattern but theme it
function applyScrollbarColors(backgroundColor, thumbColor) {
    // Create a style element if it doesn't exist
    let styleElement = document.getElementById('terminal-scrollbar-style');
    
    if (!styleElement) {
        styleElement = document.createElement('style');
        styleElement.id = 'terminal-scrollbar-style';
        document.head.appendChild(styleElement);
    }
    
    // Get the colors (use defaults if not provided)
    const bgColor = backgroundColor || '#0000AA';
    const tColor = thumbColor || '#AAAAAA';
    
    // Calculate a slightly darker shade of the background color for the pattern
    // This mimics the original blue checkered pattern but using the theme color
    function darkenColor(color) {
        // Try to convert the color to RGB components
        let r, g, b;
        
        // Handle hex colors
        if (color.startsWith('#')) {
            const hex = color.slice(1);
            // Handle both 3-digit and 6-digit hex
            if (hex.length === 3) {
                r = parseInt(hex[0] + hex[0], 16);
                g = parseInt(hex[1] + hex[1], 16);
                b = parseInt(hex[2] + hex[2], 16);
            } else {
                r = parseInt(hex.slice(0, 2), 16);
                g = parseInt(hex.slice(2, 4), 16);
                b = parseInt(hex.slice(4, 6), 16);
            }
        } 
        // Handle rgb/rgba colors
        else if (color.startsWith('rgb')) {
            const match = color.match(/(\d+)\s*,\s*(\d+)\s*,\s*(\d+)/);
            if (match) {
                r = parseInt(match[1]);
                g = parseInt(match[2]);
                b = parseInt(match[3]);
            }
        }
        
        // If we couldn't parse the color, return a default darker color
        if (r === undefined) {
            return '#000080'; // Default darker blue
        }
        
        // Darken each component by 25%
        r = Math.max(0, Math.floor(r * 0.75));
        g = Math.max(0, Math.floor(g * 0.75));
        b = Math.max(0, Math.floor(b * 0.75));
        
        // Convert back to hex
        return `#${(r).toString(16).padStart(2, '0')}${
            (g).toString(16).padStart(2, '0')}${
            (b).toString(16).padStart(2, '0')}`;
    }
    
    // Get a darker shade for the pattern
    const patternColor = darkenColor(bgColor);
    
    // Theme the scrollbar but keep the checkerboard pattern with new colors
    const cssRules = `
        .xterm .xterm-viewport::-webkit-scrollbar {
            width: 16px !important;
            background-color: ${bgColor} !important;
        }
        
        .xterm .xterm-viewport::-webkit-scrollbar-thumb {
            background-color: ${tColor} !important;
            border: 3px solid ${bgColor} !important;
            border-radius: 0 !important;
        }
        
        .xterm .xterm-viewport::-webkit-scrollbar-track {
            background-color: ${bgColor} !important;
            background-image: 
                linear-gradient(45deg, ${patternColor} 25%, transparent 25%),
                linear-gradient(-45deg, ${patternColor} 25%, transparent 25%),
                linear-gradient(45deg, transparent 75%, ${patternColor} 75%),
                linear-gradient(-45deg, transparent 75%, ${patternColor} 75%) !important;
            background-size: 4px 4px !important;
            background-position: 0 0, 0 2px, 2px -2px, -2px 0px !important;
        }
    `;
    
    // Apply the CSS
    styleElement.textContent = cssRules;
    
    console.log(`Tab ${window.tabId}: Themed scrollbar applied - Base: ${bgColor}, Pattern: ${patternColor}, Thumb: ${tColor}`);
}
// Debounced resize handler
const debouncedFitTerminal = debounce(fitTerminal, 100);

// Initialization function
function initTerminal() {
    console.log("Initializing terminal...");
    
    // Open terminal in container
    const container = document.getElementById('terminal-container');
    term.open(container);

    addContextMenuStyles();

    // At the beginning of initTerminal function, RIGHT AFTER the term.open(container) call:
console.log("Terminal opened in container, checking for tab ID");
if (typeof window.tabId === 'undefined' || !window.tabId) {
    // Alert the browser console that something's wrong with tab ID setting
    console.error("ALERT: No tab ID found - using default");
    window.tabId = "unknown-" + Date.now();
} else {
    console.log("Tab ID found: " + window.tabId);
}
    console.log("Terminal opened in container");
    
    // Initialize WebLinks
    term.webLinksInit();
    
    // Fit terminal to container
    setTimeout(() => {
        fitTerminal();
        // Log initial dimensions for debugging
        console.log(`Initial terminal size: ${term.cols}x${term.rows}`);
        
        // Store initial dimensions
        lastKnownCols = term.cols;
        lastKnownRows = term.rows;
        
        // Notify C# that the terminal is ready
        if (window.chrome && window.chrome.webview) {
            validateTabId();
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'ready',
                tabId: window.tabId
            }));
            console.log(`Ready event sent to host application for tab ${window.tabId}`);
        }
    }, 100);
    
    // Welcome message
    term.writeln('\x1B[1;3;32mSSH Terminal Component\x1B[0m');
    term.writeln('Enter connection details and click Connect to begin.');
    term.writeln('');
    
    // Resize handling
    window.addEventListener('resize', debouncedFitTerminal);
    
    // Custom resize observer to catch container size changes that might not trigger window resize
    if (window.ResizeObserver) {
        const resizeObserver = new ResizeObserver(debouncedFitTerminal);
        resizeObserver.observe(container);
    }
    
    // Function to handle terminal input (xterm.js v3.14.5 uses on('data') instead of onData)
    term.on('data', function(data) {
        if (isConnected && window.chrome && window.chrome.webview) {
            validateTabId();
            // Send the data to C# app with tab ID
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'terminal-input',
                tabId: window.tabId,
                data: data
            }));
            
            // For debugging, log short input (but don't log passwords, etc.)
            if (data.length < 3) {
                console.log(`Tab ${window.tabId}: Sent input: ${data.replace(/\r/g, '\\r').replace(/\n/g, '\\n')}`);
            } else {
                console.log(`Tab ${window.tabId}: Sent input of length ${data.length}`);
            }
        }
    });
    
    // Set up WebView2 message handler
    if (window.chrome && window.chrome.webview) {
        console.log("WebView2 detected, setting up message listener");
        window.chrome.webview.addEventListener('message', function(event) {
            console.log("Message received from host:", typeof event.data === 'string' ? 
                event.data.substring(0, Math.min(100, event.data.length)) : 'Non-string data');
            const message = event.data;
            
            try {
                // If message is a string, try to parse it as JSON
                if (typeof message === 'string' && message.trim().startsWith('{')) {
                    const parsedMessage = JSON.parse(message);
                    console.log("Parsed message type:", parsedMessage.type);
                    
                    // Check if message contains tab ID and validate it
                    if (parsedMessage.tabId && parsedMessage.tabId !== window.tabId) {
                        console.log(`Ignoring message for tab ${parsedMessage.tabId}, we are tab ${window.tabId}`);
                        return;
                    }
                    
                    if (parsedMessage.type === 'connect') {
                        // Handle connect message from C# form
                        handleConnect(parsedMessage);
                    } else if (parsedMessage.type === 'disconnect') {
                        // Handle disconnect message
                        handleDisconnect();
                    } else if (parsedMessage.type === 'settings') {
                        // Apply terminal settings
                        applyTerminalSettings(parsedMessage.settings);
                    }
                } else {
                    // If not JSON, treat as terminal data
                    // Check if this is a raw data message or base64 encoded
                    // Note: This should be updated to use tab-specific messages in the future,
                    // but keeping this for backward compatibility
                    receiveTerminalData(message);
                }
            } catch (error) {
                console.error(`Tab ${window.tabId}: Error processing message:`, error, "Message was:", message);
                // If not JSON or parsing failed, treat as terminal data
                receiveTerminalData(message);
            }
        });
    } else {
        console.warn("WebView2 not detected, communication with host will not work");
    }
    setupCustomContextMenu();
}

// Function to set up custom context menu
// Function to set up custom context menu
function setupCustomContextMenu() {
    try {
        validateTabId();
        console.log(`Tab ${window.tabId}: Setting up custom context menu`);
        
        // Create context menu elements
        const contextMenu = document.createElement('div');
        contextMenu.id = 'custom-context-menu';
        contextMenu.style.display = 'none';
        contextMenu.style.position = 'absolute';
        contextMenu.style.zIndex = '1000';
        contextMenu.style.backgroundColor = '#EEEEEE';
        contextMenu.style.border = '1px solid #000080';
        contextMenu.style.boxShadow = '2px 2px 5px rgba(0, 0, 0, 0.5)';
        contextMenu.style.padding = '2px';
        contextMenu.style.fontFamily = '"Perfect DOS VGA 437", "Consolas", monospace';
        contextMenu.style.fontSize = '14px';
        
        // Create menu items
        const menuItems = [
            { label: 'Copy', action: copyTerminalSelection },
            { label: 'Paste', action: focusAndPaste },
            { label: 'Copy & Paste', action: copyAndFocusAndPaste }
        ];
        
        // Add menu items to context menu
        menuItems.forEach(item => {
            const menuItem = document.createElement('div');
            menuItem.textContent = item.label;
            menuItem.style.padding = '4px 10px';
            menuItem.style.cursor = 'pointer';
            menuItem.style.color = '#000000';
            
            // Hover effect
            menuItem.addEventListener('mouseover', () => {
                menuItem.style.backgroundColor = '#0000AA';
                menuItem.style.color = '#FFFFFF';
            });
            
            menuItem.addEventListener('mouseout', () => {
                menuItem.style.backgroundColor = '';
                menuItem.style.color = '#000000';
            });
            
            // Click action
            menuItem.addEventListener('click', (e) => {
                e.preventDefault();
                hideContextMenu();
                try {
                    item.action();
                } catch (actionError) {
                    console.error(`Error executing ${item.label} action:`, actionError);
                    alert(`Error executing ${item.label}: ${actionError.message}`);
                }
            });
            
            contextMenu.appendChild(menuItem);
        });
        
        // Add the context menu to the document
        document.body.appendChild(contextMenu);
        
        // Function to show context menu
        function showContextMenu(x, y) {
            try {
                // Position the menu
                contextMenu.style.left = `${x}px`;
                contextMenu.style.top = `${y}px`;
                contextMenu.style.display = 'block';
                
                // Hide menu when clicking elsewhere
                setTimeout(() => {
                    window.addEventListener('click', hideContextMenu, { once: true });
                }, 0);
            } catch (error) {
                console.error("Error showing context menu:", error);
                alert(`Error showing context menu: ${error.message}`);
            }
        }
        
        // Function to hide context menu and ensure terminal gets focus
        function hideContextMenu() {
            try {
                contextMenu.style.display = 'none';
                // Explicitly focus the terminal after closing the menu
                term.focus();
            } catch (error) {
                console.error("Error hiding context menu:", error);
                alert(`Error hiding context menu: ${error.message}`);
            }
        }

        
 // Improved paste function that sends data directly through SSH channel
function focusAndPaste() {
    try {
        // Focus the terminal first
        term.focus();
        
        // Then attempt paste through the SSH data channel
        if (term && isConnected) {
            navigator.clipboard.readText().then(text => {
                if (text) {
                    // Instead of trying to paste directly into xterm.js,
                    // send the text to the C# host application
                    window.chrome.webview.postMessage(JSON.stringify({
                        type: 'terminal-input',
                        tabId: window.tabId,
                        data: text
                    }));
                    console.log(`Tab ${window.tabId}: Sent paste data to SSH channel, length: ${text.length}`);
                }
            }).catch(err => {
                console.error(`Tab ${window.tabId}: Could not paste text:`, err);
                alert(`Paste error: ${err.message}`);
            });
        }
    } catch (error) {
        console.error("Error in focusAndPaste:", error);
        alert(`Error pasting text: ${error.message}`);
    }
}

// Improved copy and paste function
function copyAndFocusAndPaste() {
    try {
        const selection = term.getSelection();
        if (selection) {
            // Copy first
            navigator.clipboard.writeText(selection).then(() => {
                // Focus terminal
                term.focus();
                // Then paste with a delay
                setTimeout(() => {
                    navigator.clipboard.readText().then(text => {
                        if (text) {
                            // Send the clipboard text through the SSH data channel
                            window.chrome.webview.postMessage(JSON.stringify({
                                type: 'terminal-input',
                                tabId: window.tabId,
                                data: text
                            }));
                            console.log(`Tab ${window.tabId}: Sent copy-paste data to SSH channel, length: ${text.length}`);
                        }
                    }).catch(err => {
                        console.error(`Tab ${window.tabId}: Could not paste text:`, err);
                        alert(`Paste error: ${err.message}`);
                    });
                }, 100);
            }).catch(err => {
                console.error(`Tab ${window.tabId}: Could not copy text:`, err);
                alert(`Copy error: ${err.message}`);
            });
        }
    } catch (error) {
        console.error("Error in copyAndFocusAndPaste:", error);
        alert(`Error copying and pasting: ${error.message}`);
    }
}
        
        // Intercept context menu event on terminal container
        const terminalContainer = document.getElementById('terminal-container');
        terminalContainer.addEventListener('contextmenu', (e) => {
            try {
                e.preventDefault();
                showContextMenu(e.clientX, e.clientY);
                return false;
            } catch (error) {
                console.error("Error handling context menu event:", error);
                alert(`Error with context menu: ${error.message}`);
            }
        });
        
        // Make sure terminal gets focus when clicked
        terminalContainer.addEventListener('mousedown', (e) => {
            try {
                // Only handle left clicks (not right clicks which trigger the context menu)
                if (e.button === 0) {
                    term.focus();
                }
            } catch (error) {
                console.error("Error handling mousedown:", error);
                // No alert here as it would be too intrusive for a common event
            }
        });
        
        // Ensure keyboard events always go to the terminal
        document.addEventListener('keydown', () => {
            try {
                // Refocus terminal on any keyboard input anywhere in the document
                if (document.activeElement !== term.textarea) {
                    term.focus();
                }
            } catch (error) {
                console.error("Error handling keydown:", error);
                // No alert here as it would be too intrusive for a common event
            }
        });
        
        console.log(`Tab ${window.tabId}: Custom context menu setup complete`);
    } catch (error) {
        console.error("Failed to set up custom context menu:", error);
        alert(`Failed to set up custom context menu: ${error.message}`);
    }
}

// Add CSS for our context menu to the terminal.html styles
function addContextMenuStyles() {
    const styleElement = document.createElement('style');
    styleElement.textContent = `
        #custom-context-menu {
            user-select: none;
            font-family: "Perfect DOS VGA 437", "Consolas", monospace;
        }
        
        #custom-context-menu div {
            white-space: nowrap;
        }
        
        #custom-context-menu div:not(:last-child) {
            border-bottom: 1px solid #CCCCCC;
        }
    `;
    document.head.appendChild(styleElement);
}

// Apply terminal settings from C#
function applyTerminalSettings(settings) {
    validateTabId();
    console.log(`Tab ${window.tabId}: Applying terminal settings:`, settings);
    
    if (!settings) return;
    
    // For xterm.js v3, we need to recreate the terminal with new options
    // since many options cannot be changed after creation
    const newOptions = { ...term.getOption() };
    
    if (settings.fontSize) {
        newOptions.fontSize = settings.fontSize;
        term.setOption('fontSize', settings.fontSize);
    }
    
    if (settings.fontFamily) {
        newOptions.fontFamily = settings.fontFamily;
        term.setOption('fontFamily', settings.fontFamily);
    }
    
    if (settings.cursorBlink !== undefined) {
        newOptions.cursorBlink = settings.cursorBlink;
        term.setOption('cursorBlink', settings.cursorBlink);
    }
    
    if (settings.scrollback) {
        newOptions.scrollback = settings.scrollback;
        term.setOption('scrollback', settings.scrollback);
    }
    
    if (settings.isDarkTheme !== undefined) {
        const theme = settings.isDarkTheme 
            ? {
                background: '#1e1e1e',
                foreground: '#f0f0f0',
                cursor: '#ffffff'
            } 
            : {
                background: '#f0f0f0',
                foreground: '#1e1e1e',
                cursor: '#000000'
            };
        
        newOptions.theme = theme;
        term.setOption('theme', theme);
    }
    
    // Apply changes and resize
    fitTerminal();
}

// Handle connect initiated from C#
function handleConnect(data) {
    validateTabId();
    console.log(`Tab ${window.tabId}: Handle connect:`, data);
    
    if (!data.host) {
        term.writeln('\r\n\x1B[1;3;31mError: Host is required\x1B[0m');
        return;
    }
    
    if (!data.username) {
        term.writeln('\r\n\x1B[1;3;31mError: Username is required\x1B[0m');
        return;
    }
    
    // Show connecting message
    term.writeln(`\r\n\x1B[1;3;33mConnecting to ${data.username}@${data.host}:${data.port || '22'}...\x1B[0m`);
}

// Handle disconnect
function handleDisconnect() {
    validateTabId();
    console.log(`Tab ${window.tabId}: Handle disconnect`);
    isConnected = false;
    term.writeln('\r\n\x1B[1;3;33mDisconnected\x1B[0m');
}

// Function to be called when SSH connection is established
function terminalConnected() {
    validateTabId();
    console.log(`Tab ${window.tabId}: Terminal connected`);
    isConnected = true;
    // term.writeln('\x1B[1;3;32mConnection established\x1B[0m');
    
    // Make sure terminal is properly sized after connection
    setTimeout(() => {
        fitTerminal();
        // Force a resize report to ensure server has correct dimensions
        reportTerminalSize();
    }, 100);
}

// Function to handle connection failure
function connectionFailed(errorMessage) {
    validateTabId();
    console.log(`Tab ${window.tabId}: Connection failed:`, errorMessage);
    isConnected = false;
    term.writeln(`\r\n\x1B[1;3;31mConnection failed: ${errorMessage}\x1B[0m`);
}

// Function to receive data from SSH and write to terminal
function receiveTerminalData(data) {
    validateTabId();
    if (term) {
        // For debugging, log first bits of received data
        const logData = typeof data === 'string' && data.length > 50 ? 
            data.substring(0, 50) + '...' : data;
        console.log(`Tab ${window.tabId}: Received data:`, logData);
        term.write(data);
    }
}

// Function to copy terminal selection to clipboard
function copyTerminalSelection() {
    validateTabId();
    if (term) {
        const selection = term.getSelection();
        if (selection) {
            navigator.clipboard.writeText(selection).catch(err => {
                console.error(`Tab ${window.tabId}: Could not copy text:`, err);
            });
        }
    }
}

// Function to paste text from clipboard to terminal
function pasteToTerminal() {
    validateTabId();
    if (term && isConnected) {
        navigator.clipboard.readText().then(text => {
            term.paste(text);
        }).catch(err => {
            console.error(`Tab ${window.tabId}: Could not paste text:`, err);
        });
    }
}

/**
 * Function to receive base64 encoded data from SSH and write to terminal
 * This avoids character escaping issues that can cause syntax errors
 */
function receiveTerminalDataEncoded(base64Data) {
    validateTabId();
    if (!term) return;
    
    try {
        // Decode the base64 string
        const decoded = atob(base64Data);
        
        // Write the decoded data to the terminal
        term.write(decoded);
    } catch (error) {
        console.error(`Tab ${window.tabId}: Error decoding terminal data:`, error);
    }
}

// Function to clear the terminal screen
function clearTerminal() {
    validateTabId();
    if (term) {
        term.clear();
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    console.log("DOM content loaded, initializing terminal");
    initTerminal();
});