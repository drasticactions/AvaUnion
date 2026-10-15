import QtQuick
import QtQuick.Controls
import QtQuick.Layouts
import org.kde.kirigami as Kirigami

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
        spacing: 16
        Label { text: "Inline messages"; font.bold: true }
        Kirigami.InlineMessage { visible: true; type: Kirigami.MessageType.Information; text: "Information: the backup finished."; Layout.preferredWidth: 350 }
        Kirigami.InlineMessage { visible: true; type: Kirigami.MessageType.Positive; text: "Saved to Documents."; Layout.preferredWidth: 350 }
        Kirigami.InlineMessage { visible: true; type: Kirigami.MessageType.Warning; text: "The disk is almost full."; Layout.preferredWidth: 350 }
        Kirigami.InlineMessage { visible: true; type: Kirigami.MessageType.Error; text: "The file could not be opened."; Layout.preferredWidth: 350 }
        Button { text: "Show a notification" }
        Label { text: "Command bar"; font.bold: true }
        ToolBar {
            Layout.fillWidth: true
            RowLayout {
                anchors.right: parent.right
                ToolButton { text: "New" }
                ToolButton { text: "Open" }
                ToolSeparator {}
                ToolButton { text: "Bold"; checkable: true; checked: true }
                ToolButton { icon.name: "overflow-menu" }
            }
        }
        Label { text: "Auto-complete, pages and splitters"; font.bold: true }
        TextField { placeholderText: "A state"; Layout.preferredWidth: 220 }
        PageIndicator { count: 5; currentIndex: 1 }
        SplitView {
            Layout.preferredWidth: 241
            Layout.preferredHeight: 60
            Rectangle { SplitView.preferredWidth: 120; color: palette.base }
            Rectangle { color: palette.base }
        }
    }
}
