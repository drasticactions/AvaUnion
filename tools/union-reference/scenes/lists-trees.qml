import QtQuick
import QtQuick.Controls
import QtQuick.Layouts
import Qt.labs.qmlmodels
import org.kde.kirigami as Kirigami
import org.kde.union.impl as Union

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
        Label { text: "List box"; font.bold: true }
        ScrollView {
            Layout.preferredWidth: 260
            Layout.preferredHeight: 220
            Union.StyleHints.showFramedBackground: true
            Kirigami.Theme.colorSet: Kirigami.Theme.View
            Kirigami.Theme.inherit: false
                ListView {
                    clip: true
                    model: ["Ardennes", "Black Forest", "Carpathians", "Dolomites", "Eifel", "Fichtelgebirge", "Giant Mountains", "Harz", "Disabled"]
                    currentIndex: 1
                    delegate: ItemDelegate {
                        required property string modelData
                        required property int index
                        width: ListView.view.width
                        text: modelData
                        enabled: modelData !== "Disabled"
                        highlighted: ListView.isCurrentItem
                        onClicked: ListView.view.currentIndex = index
                    }
                }
        }
        Label { text: "Tree view"; font.bold: true }
        ScrollView {
            Layout.preferredWidth: 260
            Layout.preferredHeight: 180
            Union.StyleHints.showFramedBackground: true
            Kirigami.Theme.colorSet: Kirigami.Theme.View
            Kirigami.Theme.inherit: false
            TreeView {
                id: tree
                clip: true
                columnWidthProvider: () => tree.width
                model: TreeModel {
                    TableModelColumn { display: "name" }
                    rows: [
                        { name: "Documents", rows: [ { name: "report.odt" }, { name: "Archive", rows: [ { name: "old.txt" } ] } ] },
                        { name: "Music" },
                        { name: "Pictures" }
                    ]
                }
                delegate: TreeViewDelegate {}
                Component.onCompleted: expand(0)
            }
        }
        Label { text: "Combo box"; font.bold: true }
        ComboBox { model: ["Kate", "Konsole", "Dolphin"]; Layout.preferredWidth: 200 }
        ComboBox { editable: true; model: ["One", "Two"]; Layout.preferredWidth: 200; Component.onCompleted: editText = "Editable" }
    }
}
