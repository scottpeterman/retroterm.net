# RetroTerm.NET - Architecture and Implementation Documentation

## Overview

RetroTerm.NET is a tabbed terminal emulator with a retro UI inspired by classic DOS/Borland applications. It allows users to manage multiple SSH connections simultaneously through a tabbed interface while maintaining a nostalgic aesthetic.

## Project Structure and Component Breakdown

### Core Components

#### 1. Models

**Theme.cs**
- Purpose: Defines the color scheme and appearance for the UI and terminal
- Key features:
  - Theme metadata (name, description, author)
  - UI color definitions (backgrounds, text, buttons, menus)
  - Terminal color definitions (background, foreground, cursor, ANSI colors)
  - Color conversion utilities (hex to Color and vice versa)
  - Theme mode indicator (dark/light)

**ConnectionProfile.cs**
- Purpose: Represents a saved SSH connection configuration
- Key features:
  - Connection details (host, port, username, password)
  - Display and metadata (name, created date, last accessed)
  - Usage tracking (count of connections)
  - Authentication options (password or key-based)

#### 2. Services

**ThemeManager.cs**
- Purpose: Manages loading, saving, and applying themes
- Key features:
  - Built-in theme definitions (Borland Classic, Light Theme, etc.)
  - Loading themes from JSON files
  - Theme application events
  - Import/export functionality
  - Theme searching and selection

**TabManager.cs**
- Purpose: Manages creation, deletion, and state of terminal tabs
- Key features:
  - Tab creation and closing
  - Tab selection handling
  - Connection handling for tabs
  - Theme application to all tabs
  - Event propagation from tabs
  - Special "+" tab for creating new terminals

**SettingsManager.cs**
- Purpose: Manages application settings persistence
- Key features:
  - Registry-based settings storage
  - Default values for first run
  - Window size/position remembering
  - Theme selection persistence
  - Sound effect toggle persistence

**ConnectionDirectoryService.cs**
- Purpose: Manages saved SSH connection profiles
- Key features:
  - File-based profile storage (JSON)
  - CRUD operations for profiles
  - Profile searching and filtering
  - Event notifications for changes

#### 3. Controls

**TerminalTabPage.cs**
- Purpose: Custom TabPage that encapsulates a terminal control
- Key features:
  - Embeds SshTerminalControl
  - Handles connection state
  - Manages theme application
  - Provides panel with double-line border (DOS style)
  - Connection/disconnection handling
  - Event propagation

#### 4. Forms

**MainForm.cs**
- Purpose: Main application window with tabbed interface
- Key features:
  - Tab control for terminal sessions
  - Custom-drawn tabs with close buttons
  - DOS-style menu bar
  - Borland-style function key bar
  - Toolbar with connection buttons
  - Status bar with theme-aware styling
  - Window chrome with custom border
  - Draggable title area
  - Keyboard shortcuts

**ConnectionDialog.cs**
- Purpose: Dialog for entering SSH connection details
- Key features:
  - Theme-aware UI
  - Form validation
  - Connection saving option
  - Draggable title bar
  - Double-line border styling

**ConnectionDirectoryForm.cs**
- Purpose: Dialog for managing saved connections
- Key features:
  - List of saved connections
  - Search/filter functionality
  - Connection management (create, edit, delete)
  - Connection selection
  - Theme-aware styling

### Integration with Existing Components

RetroTerm.NET integrates with two key existing components:

1. **SshTerminalComponent**:
   - Provides the core terminal functionality (SshTerminalControl)
   - SSH connection handling and data transfer
   - Terminal rendering and input/output
   - Used within the TerminalTabPage to enable terminal functionality

2. **SessionNavigatorControl**:
   - Session tree view and management
   - Would be integrated in future versions for enhanced session management
   - Currently not directly used, but compatibility is maintained

## Design Patterns and Architecture

1. **Model-View-Controller Pattern**:
   - Models: Theme, ConnectionProfile
   - Views: MainForm, ConnectionDialog, ConnectionDirectoryForm
   - Controllers: ThemeManager, TabManager, ConnectionDirectoryService

2. **Service Pattern**:
   - Services abstract functionality from UI
   - Promotes separation of concerns
   - Makes testing and maintenance easier

3. **Composition over Inheritance**:
   - TerminalTabPage composes SshTerminalControl rather than inheriting
   - TabManager composes TerminalTabPage instances
   - Increases flexibility and maintainability

4. **Event-Based Communication**:
   - Components communicate through events (ThemeChanged, ConnectionStateChanged, etc.)
   - Reduces tight coupling between components
   - Makes the system more modular and extensible

## Visual Design Elements

1. **Borland-Style UI**:
   - Blue background with cyan accents
   - White text with yellow input fields
   - Red function key indicators
   - Double-line borders
   - Gray menu bar with red hotkey indicators

2. **Tab Design**:
   - Custom-drawn tabs with theme-aware colors
   - Selected tab has different colors than unselected tabs
   - Close button (X) in each tab
   - "+" tab for creating new terminals

3. **Function Key Bar**:
   - Bottom panel showing available function keys
   - Red key indicators with black descriptions
   - Matches classic DOS application style

4. **Status Bar**:
   - Shows current status (connected, disconnected)
   - Theme-aware styling
   - Double-line border with title

## Key Workflows

1. **Opening a New Terminal Tab**:
   - Click the "+" tab or press Ctrl+T
   - TabManager creates a new TerminalTabPage
   - Applies the current theme
   - Tab is selected automatically

2. **Connecting to SSH**:
   - Click "CONNECT" button or open connection directory
   - Enter connection details or select a saved profile
   - Connection parameters are applied to the current tab
   - Terminal establishes SSH connection
   - Tab title updates to show connection information
   - Status bar updates to show connected state

3. **Switching Between Sessions**:
   - Click on a tab to switch to that session
   - TabManager handles the selection change
   - Status bar updates to show the current tab's connection status

4. **Applying Themes**:
   - Select a theme from the Terminal > Themes menu
   - ThemeManager loads and applies the theme
   - TabManager applies theme to all terminal tabs
   - UI elements update with new colors
   - Terminal colors are updated

5. **Managing Saved Connections**:
   - Open connection directory
   - View list of saved connections
   - Create, edit, or delete connections
   - Filter connections by search term
   - Select and connect to a saved profile

## Future Improvements

1. **Layout Refinements**:
   - Improve spacing and alignment
   - Fix the 170+ warnings for better code quality
   - Enhance UI responsiveness

2. **Session Navigator Integration**:
   - Integrate the existing SessionNavigator component
   - Enable tree-based session management
   - Support folder organization for connections

3. **Split Panes**:
   - Allow splitting a tab to view multiple terminals
   - Enable side-by-side terminal sessions
   - Support different layouts within tabs

4. **Session Recording**:
   - Add capability to record terminal sessions
   - Implement playback functionality
   - Support saving recordings to file

5. **Enhanced Themes**:
   - Add more theme customization options
   - Support creating themes in the UI
   - Implement theme import/export

## Conclusion

RetroTerm.NET builds upon existing components to create a cohesive, tabbed terminal experience with a retro aesthetic. The architecture follows modern patterns while the UI maintains a nostalgic feel. The modular design allows for future extensions and improvements while preserving compatibility with existing components.

The implementation successfully merges modern functionality (tabbed interface, theme support, connection management) with a classic DOS/Borland-inspired visual style, creating a unique terminal emulator that stands out in both appearance and functionality.