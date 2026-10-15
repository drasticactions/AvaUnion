import QtQuick
import QtQuick.Controls
import QtQuick.Layouts
import Qt.labs.qmlmodels

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
        width: 868
        Label { text: "Data grid (AvaUnion.DataGrid)"; font.bold: true }
        CheckBox { text: "Alternating row colors"; checked: true }
        Frame {
            padding: 1
            Layout.fillWidth: true
            Layout.preferredHeight: 240
            GridLayout {
                anchors.fill: parent
                columns: 2
                rowSpacing: 0
                columnSpacing: 0
                Item { implicitWidth: rows.width; implicitHeight: columns.height }
                HorizontalHeaderView { id: columns; syncView: table; Layout.fillWidth: true; clip: true; model: ["Name", "City", "Born"] }
                VerticalHeaderView { id: rows; syncView: table; Layout.fillHeight: true; clip: true; model: 8; delegate: VerticalHeaderViewDelegate { text: "" } }
                TableView {
                    id: table
                    Layout.fillWidth: true
                    Layout.fillHeight: true
                    clip: true
                    columnWidthProvider: c => [180, 160, 400][c]
                    selectionModel: ItemSelectionModel {}
                    model: TableModel {
                        TableModelColumn { display: "name" }
                        TableModelColumn { display: "city" }
                        TableModelColumn { display: "born" }
                        rows: [
                            { name: "Ada Lovelace", city: "London", born: "1815" },
                            { name: "Grace Hopper", city: "New York", born: "1906" },
                            { name: "Linus Torvalds", city: "Helsinki", born: "1969" },
                            { name: "Margaret Hamilton", city: "Paoli", born: "1936" },
                            { name: "Ken Thompson", city: "New Orleans", born: "1943" },
                            { name: "Barbara Liskov", city: "Los Angeles", born: "1939" },
                            { name: "Matthias Ettrich", city: "Bietigheim-Bissingen", born: "1972" },
                            { name: "Frances Allen", city: "Peru, New York", born: "1932" }
                        ]
                    }
                    delegate: TableViewDelegate {}
                    Component.onCompleted: selectionModel.select(model.index(0, 0), ItemSelectionModel.Select | ItemSelectionModel.Rows)
                }
            }
        }
    }
}
