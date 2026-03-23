import sys
import subprocess
import os
import io

try:
    from PIL import Image
    import PIL
except ImportError:
    subprocess.check_call([sys.executable, "-m", "pip", "install", "Pillow"])
    from PIL import Image

try:
    from rembg import remove
except ImportError:
    subprocess.check_call([sys.executable, "-m", "pip", "install", "rembg", "onnxruntime"])
    from rembg import remove

def process(in_path, out_path, target_width, target_height):
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
        
    # Resize to exact dimensions (360x320)
    # We will use LANCZOS but it will stretch exactly to 360x320
    final_img = img.resize((target_width, target_height), Image.Resampling.LANCZOS)
    final_img.save(out_path, "PNG")

in_path = r"C:\Users\comes\.gemini\antigravity\brain\e4586017-9d0a-4ac6-8aeb-3880212673ac\hud_panel_GENERATETHIS.png"
out_path = r"c:\Project\Match3\Assets\Resources\hud_panel.png"

# This will be replaced by the actual path later, or I'll just pass it as arguments
if __name__ == "__main__":
    if len(sys.argv) > 2:
        in_path = sys.argv[1]
        out_path = sys.argv[2]
    print(f"Processing {in_path} -> {out_path}")
    process(in_path, out_path, 360, 320)
    print("All done!")
