import bpy
import os
from math import radians

# Function to create folders if they don't exist
def create_folder(path):
    if not os.path.exists(path):
        os.makedirs(path)

# Function to set camera transform for a specific direction
def set_camera_transform(location, rotation):
    camera.location = location
    camera.rotation_euler = rotation
    bpy.context.view_layer.update()

# Function to render animation for a specific direction
def render_animation(direction):
    folder_path = os.path.join(base_directory, f"Attack{direction}")
    create_folder(folder_path)
    bpy.context.scene.render.filepath = os.path.join(folder_path, "frame_")
    bpy.ops.render.render(animation=True, write_still=True)

# Base directory for saving the folders
base_directory = r"C:\AttackAnimations"

# Gather camera in scene
camera = bpy.context.scene.camera

# Set the start and end frames for the animation
start_frame = 1
end_frame = 23 
bpy.context.scene.frame_start = start_frame
bpy.context.scene.frame_end = end_frame

# Render settings
bpy.context.scene.render.image_settings.file_format = 'PNG'

# Define camera positions and rotations for each direction
camera_settings = {
    "South": {
        "location": (0, -3.4, 7.5),
        "rotation": (radians(25), radians(0), radians(0))
        
    },
    "SouthWest": {
        "location": (2, -2.38, 7.5),
        "rotation": (radians(25), radians(0), radians(-315))
    },
    "West": {
        "location": (3.2, 0, 7.5),
        "rotation": (radians(25), radians(0), radians(-270))
    },
    "NorthWest": {
        "location": (2.3, 2, 7.5),
        "rotation": (radians(25), radians(0), radians(-225))
    },
    "North": {
        "location": (0.15, 3.15, 7.5),
        "rotation": (radians(25), radians(0), radians(-180))
    },
    "NorthEast": {
        "location": (-2.05, 2.16, 7.5),
        "rotation": (radians(25), radians(0), radians(-135))
    },
    "East": {
        "location": (-3, -.15, 7.5),
        "rotation": (radians(25), radians(0), radians(270))
    },
    "SouthEast": {
        "location": (-2.5, -2.5, 7.5),
        "rotation": (radians(25), radians(0), radians(-45))
    },
}

# Render the animation for each direction
for direction, settings in camera_settings.items():
    set_camera_transform(settings["location"], settings["rotation"])
    render_animation(direction)

print("Rendering complete.")
