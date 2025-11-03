import dearpygui.dearpygui as dpg

# Fake scene data
_entities = [
    {"id": 1, "name": "Camera"},
    {"id": 2, "name": "Player"},
    {"id": 3, "name": "Light"},
    {"id": 4, "name": "Ground"}
]

selected_entity = None
_selection_callback = None


def set_selection_callback(callback):
    global _selection_callback
    _selection_callback = callback


def build_panel():
    with dpg.child_window(label="Hierarchy", width=250, autosize_y=True):
        dpg.add_text("Scene Hierarchy")
        dpg.add_separator()

        for entity in _entities:
            dpg.add_selectable(
                label=entity["name"],
                callback=lambda s, a, u=entity: _on_select(u)
            )


def _on_select(entity):
    global selected_entity
    selected_entity = entity
    print(f"Selected entity: {entity['name']}")
    if _selection_callback:
        _selection_callback(entity)
