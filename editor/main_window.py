import dearpygui.dearpygui as dpg
from editor.panels import hierarchy, inspector, scene_manager as sm

def build_main_window():
    with dpg.window(label="PyFlare Editor", tag="MainWindow", width=1280, height=720):
        dpg.add_text("PyFlare Editor v0.0.3")
        dpg.add_separator()
        # Toolbar buttons
        dpg.add_button(label="New Scene", callback=lambda: _new_scene())
        dpg.add_button(label="Add Entity", callback=lambda: _add_entity())
        dpg.add_button(label="Save Scene", callback=lambda: sm.save_scene("scene.json"))
        dpg.add_button(label="Load Scene", callback=lambda: sm.load_scene("scene.json") or _refresh_hierarchy())
        dpg.add_separator()

        with dpg.group(horizontal=True):
            hierarchy.build_panel()
            with dpg.child_window(label="Viewport", autosize_x=True, autosize_y=True):
                dpg.add_text("Viewport placeholder")
            inspector.build_panel()

        hierarchy.set_selection_callback(inspector.update_inspector)

def _new_scene():
    sm.create_scene("NewScene")
    _refresh_hierarchy()

def _add_entity():
    sm.add_entity(f"Entity{len(sm.get_entities())+1}")
    _refresh_hierarchy()

def _refresh_hierarchy():
    hierarchy._refresh()