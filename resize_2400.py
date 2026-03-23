import os
import sys
from PIL import Image

def resize_to_fill(img, target_width, target_height):
    target_ratio = target_width / target_height
    img_ratio = img.width / img.height
    
    if img_ratio > target_ratio:
        # Image is too wide. Crop left and right.
        new_width = int(target_ratio * img.height)
        offset = (img.width - new_width) // 2
        crop_box = (offset, 0, offset + new_width, img.height)
    else:
        # Image is too tall. Crop top and bottom.
        new_height = int(img.width / target_ratio)
        offset = (img.height - new_height) // 2
        crop_box = (0, offset, img.width, offset + new_height)
        
    img_cropped = img.crop(crop_box)
    return img_cropped.resize((target_width, target_height), Image.Resampling.LANCZOS)

input_path = sys.argv[1]
output_path = r"c:\Project\Match3\Assets\Resources\level_select_bg.png"

os.makedirs(os.path.dirname(output_path), exist_ok=True)
img = Image.open(input_path).convert("RGBA")
img = resize_to_fill(img, 1080, 2400)
img.save(output_path, "PNG")
print(f"Saved {img.size} to {output_path}")
