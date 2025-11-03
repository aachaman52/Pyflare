import dearpygui.dearpygui as dpg

_current = None

def build_panel():
    with dpg.child_window(label="Inspector", width=300, autosize_y=True):
        dpg.add_text("Inspector")
        dpg.add_separator()
        # placeholder for entity info
        dpg.add_text("Select an object to inspect.", tag="inspector_text")

        # Position sliders
        dpg.add_text("Position:", tag="pos_label")
        dpg.add_input_floatx(label="X", tag="pos_x", width=200, callback=_update_pos)
        dpg.add_input_floatx(label="Y", tag="pos_y", width=200, callback=_update_pos)
        dpg.add_input_floatx(label="Z", tag="pos_z", width=200, callback=_update_pos)

def update_inspector(entity):
    global _current
    _current = entity
    dpg.set_value("inspector_text", f"Inspecting: {entity['name']}")

    # Initialize sliders with current values (fake data for now)
    pos = entity.get("position", [0.0, 0.0, 0.0])
    dpg.set_value("pos_x", pos[0])
    dpg.set_value("pos_y", pos[1])
    dpg.set_value("pos_z", pos[2])

def _update_pos(sender, app_data, user_data):
    if not _current:
        return
    # Update entity position
    _current["position"] = [
        dpg.get_value("pos_x"),
        dpg.get_value("pos_y"),
        dpg.get_value("pos_z")
    ]
    print(f"{_current['name']} position updated: {_current['position']}")
