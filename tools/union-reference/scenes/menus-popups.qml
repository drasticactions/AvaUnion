import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

Pane {
    LayoutMirroring.enabled: Qt.application.layoutDirection === Qt.RightToLeft
    LayoutMirroring.childrenInherit: true
    width: 900
    height: 700
    leftPadding: 16
    rightPadding: 16
    topPadding: 16
    bottomPadding: 16
    ColumnLayout {
        spacing: 12
        Label { text: "Menu bar"; font.bold: true }
        MenuBar {
            id: bar
            Menu {
                id: file
                title: "&File"
                Action { text: "&New"; shortcut: "Ctrl+N" }
                Action { text: "&Open…"; shortcut: "Ctrl+O" }
                MenuSeparator {}
                Menu { title: "Recent"; Action { text: "report.odt" } Action { text: "notes.txt" } }
                MenuSeparator {}
                Action { text: "&Quit"; shortcut: "Ctrl+Q" }
            }
            Menu {
                title: "&View"
                Action { text: "Show toolbar"; checkable: true; checked: true }
            }
            Menu { title: "&Help" }
        }
        Label { text: "Context menu and tool tip"; font.bold: true }
        Frame {
            Layout.preferredWidth: 300
            Layout.preferredHeight: 80
            Label { anchors.centerIn: parent; text: "Right-click here" }
        }
    }
    Menu {
        id: context
        x: 420
        y: 60
        Action { text: "Cut"; shortcut: "Ctrl+X" }
        Action { text: "Copy"; shortcut: "Ctrl+C" }
        Action { text: "Paste"; shortcut: "Ctrl+V"; enabled: false }
    }
    ToolTip { x: 420; y: 260; visible: true; text: "Right-click for a context menu" }
    Component.onCompleted: context.open()
}
