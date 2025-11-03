import dearpygui.dearpygui as dpg
from .scene_manager import get_entities

selected_entity = None
_selection_callback = None

def set_selection_callback(callback):
    global _selection_callback
    _selection_callback = callback

def build_panel():
    with dpg.child_window(label="Hierarchy", width=250, autosize_y=True):
        dpg.add_text("Scene Hierarchy")
        dpg.add_separator()
        _refresh()

def _refresh():
    # Clear old items
    for item in dpg.get_all_items():
        if dpg.get_item_label(item) not in ["Scene Hierarchy"]:
            dpg.delete_item(item)

    for entity in get_entities():
        dpg.add_selectable(
            label=entity["name"],
            callback=lambda s, a, u=entity: _on_select(u)
        )

def _on_select(entity):
    global selected_entity
    selected_entity = entity
    if _selection_callback:
        _selection_callback(entity)