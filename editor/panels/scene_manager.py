import json

_scenes = {}
_active_scene = None
_entity_counter = 0

def create_scene(name):
    global _active_scene, _entity_counter
    _active_scene = {"scene_name": name, "entities": []}
    _scenes[name] = _active_scene
    _entity_counter = 0
    return _active_scene

def add_entity(name, components=None):
    global _entity_counter
    if components is None:
        components = []
    _entity_counter += 1
    entity = {
        "id": _entity_counter,
        "name": name,
        "position": [0.0, 0.0, 0.0],
        "rotation": [0.0, 0.0, 0.0],
        "scale": [1.0, 1.0, 1.0],
        "components": components
    }
    _active_scene["entities"].append(entity)
    return entity

def get_entities():
    return _active_scene["entities"] if _active_scene else []

def save_scene(file_path):
    if _active_scene:
        with open(file_path, "w") as f:
            json.dump(_active_scene, f, indent=4)

def load_scene(file_path):
    global _active_scene, _entity_counter
    with open(file_path, "r") as f:
        _active_scene = json.load(f)
    _entity_counter = max([e["id"] for e in _active_scene["entities"]], default=0)
    return _active_scene