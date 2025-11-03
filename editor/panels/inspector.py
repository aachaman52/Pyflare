import dearpygui.dearpygui as dpg

_current = None

def build_panel():
    with dpg.child_window(label="Inspector", width=300, autosize_y=True):
        dpg.add_text("Inspector")
        dpg.add_separator()
        dpg.add_text("Select an object to inspect.", tag="inspector_text")

def update_inspector(entity):
    global _current
    _current = entity
    dpg.set_value("inspector_text", f"Inspecting: {entity['name']}")
