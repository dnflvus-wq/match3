import sys
import subprocess
import os

try:
    from PIL import Image
except ImportError:
    subprocess.check_call([sys.executable, "-m", "pip", "install", "Pillow"])
    from PIL import Image

try:
    from rembg import remove
except ImportError:
    subprocess.check_call([sys.executable, "-m", "pip", "install", "rembg", "onnxruntime"])
    from rembg import remove

import io

def process(in_path, out_path):
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    with open(in_path, 'rb') as i:
        input_data = i.read()
    
    # remove background smoothly
    output_data = remove(input_data)
    img = Image.open(io.BytesIO(output_data)).convert("RGBA")
    
    # Crop to bounding box of content
    bbox = img.getbbox()
    if bbox:
        img = img.crop(bbox)
        
    # Make it exactly square
    max_dim = max(img.width, img.height)
    square_img = Image.new("RGBA", (max_dim, max_dim), (0, 0, 0, 0))
    square_img.paste(img, ((max_dim - img.width) // 2, (max_dim - img.height) // 2))
    
    # Resize to 110x110
    final_img = square_img.resize((110, 110), Image.Resampling.LANCZOS)
    final_img.save(out_path, "PNG")

paths = [
    (r"C:\Users\comes\.gemini\antigravity\brain\e4586017-9d0a-4ac6-8aeb-3880212673ac\red_candy_1774058868916.png", r"c:\Project\Match3\Assets\Textures\Sprites\fish_3.png"),
    (r"C:\Users\comes\.gemini\antigravity\brain\e4586017-9d0a-4ac6-8aeb-3880212673ac\blue_candy_1774058885724.png", r"c:\Project\Match3\Assets\Textures\Sprites\fish_4.png"),
    (r"C:\Users\comes\.gemini\antigravity\brain\e4586017-9d0a-4ac6-8aeb-3880212673ac\green_candy_1774058904713.png", r"c:\Project\Match3\Assets\Textures\Sprites\fish_5.png")
]

for in_path, out_path in paths:
    print(f"Processing {in_path} to {out_path}")
    process(in_path, out_path)
print("All done!")
