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
        width: 420
        Label { text: "Text fields"; font.bold: true }
        TextField { text: "Editable text"; Layout.fillWidth: true }
        TextField { placeholderText: "Placeholder text"; Layout.fillWidth: true }
        TextField { text: "Read only"; readOnly: true; Layout.fillWidth: true }
        TextField { text: "Disabled"; enabled: false; Layout.fillWidth: true }
        TextField { text: "secret"; echoMode: TextInput.Password; Layout.fillWidth: true }
        Label { text: "Spin boxes"; font.bold: true }
        SpinBox { from: 0; to: 10; value: 5; editable: true; Layout.fillWidth: true }
        SpinBox { from: 0; to: 10; value: 10; editable: true; Layout.fillWidth: true }
        Label { text: "Text area"; font.bold: true }
        TextArea { text: "A text area accepts returns.\nIt uses Union's textarea element."; wrapMode: TextEdit.Wrap; Layout.fillWidth: true; Layout.preferredHeight: 100 }
    }
}
