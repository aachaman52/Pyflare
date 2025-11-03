import dearpygui.dearpygui as dpg
from editor.panels import hierarchy, inspector


def build_main_window():
    with dpg.window(label="PyFlare Editor", tag="MainWindow", width=1280, height=720):
        dpg.add_text("PyFlare Editor v0.0.2 — Hierarchy & Inspector")
        dpg.add_separator()

        # Split main layout into 3 columns: hierarchy, viewport placeholder, inspector
        with dpg.group(horizontal=True):
            hierarchy.build_panel()
            with dpg.child_window(label="Viewport", autosize_x=True, autosize_y=True):
                dpg.add_text("Viewport placeholder (scene preview will go here)")
            inspector.build_panel()

        # Link hierarchy to inspector
        hierarchy.set_selection_callback(inspector.update_inspector)
