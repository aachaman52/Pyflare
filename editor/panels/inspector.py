import dearpygui.dearpygui as dpg

_current = None

def build_panel():
    with dpg.child_window(label="Inspector", width=300, autosize_y=True):
        dpg.add_text("Inspector")
        dpg.add_separator()
        dpg.add_text("Select an object to inspect.", tag="inspector_text")

        # Position
        dpg.add_text("Position:")
        for i, axis in enumerate(["X","Y","Z"]):
            dpg.add_input_float(label=axis, tag=f"pos_{axis.lower()}", callback=_update_transform)
        # Rotation
        dpg.add_text("Rotation:")
        for i, axis in enumerate(["X","Y","Z"]):
            dpg.add_input_float(label=axis, tag=f"rot_{axis.lower()}", callback=_update_transform)
        # Scale
        dpg.add_text("Scale:")
        for i, axis in enumerate(["X","Y","Z"]):
            dpg.add_input_float(label=axis, tag=f"scl_{axis.lower()}", callback=_update_transform)

def update_inspector(entity):
    global _current
    _current = entity
    dpg.set_value("inspector_text", f"Inspecting: {entity['name']}")
    # Populate values
    for i, axis in enumerate(["x","y","z"]):
        dpg.set_value(f"pos_{axis}", entity["position"][i])
        dpg.set_value(f"rot_{axis}", entity["rotation"][i])
        dpg.set_value(f"scl_{axis}", entity["scale"][i])

def _update_transform(sender, app_data, user_data):
    if not _current:
        return
    for i, axis in enumerate(["x","y","z"]):
        _current["position"][i] = dpg.get_value(f"pos_{axis}")
        _current["rotation"][i] = dpg.get_value(f"rot_{axis}")
        _current["scale"][i] = dpg.get_value(f"scl_{axis}")