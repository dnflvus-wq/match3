import sys
import subprocess
import os
import io

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
        
    # Make it exactly square centered
    max_dim = max(img.width, img.height)
    square_img = Image.new("RGBA", (max_dim, max_dim), (0, 0, 0, 0))
    square_img.paste(img, ((max_dim - img.width) // 2, (max_dim - img.height) // 2))
    
    # Resize to 256x256
    final_img = square_img.resize((256, 256), Image.Resampling.LANCZOS)
    final_img.save(out_path, "PNG")

paths = [
    (r"C:\Users\comes\.gemini\antigravity\brain\e4586017-9d0a-4ac6-8aeb-3880212673ac\level_btn_1774175036664.png", r"c:\Project\Match3\Assets\Resources\level_btn.png"),
    (r"C:\Users\comes\.gemini\antigravity\brain\e4586017-9d0a-4ac6-8aeb-3880212673ac\level_btn_locked_1774175049847.png", r"c:\Project\Match3\Assets\Resources\level_btn_locked.png")
]

for in_path, out_path in paths:
    print(f"Processing {in_path} to {out_path}")
    process(in_path, out_path)
print("All done!")
