import dearpygui.dearpygui as dpg
from editor.main_window import build_main_window

def run_app():
    dpg.create_context()
    dpg.create_viewport(title="PyFlare Editor", width=1280, height=720)
    dpg.setup_dearpygui()

    build_main_window()

    dpg.show_viewport()
    dpg.set_primary_window("MainWindow", True)
    dpg.start_dearpygui()
    dpg.destroy_context()
    
