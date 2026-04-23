using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeGameLibrary
{
    // Handles image sequence logic and file path management for the splash screen.
    // (OOP: Abstraction) This class hides the complexity of file indexing and path concatenation from the UI.
    public class ImageSequenceManager
    {
        // (OOP: Encapsulation) These fields store the internal state and are kept private.
        private readonly string _assetsFolder;
        private int _imageIndex;
        private readonly int _maxImages;

        //The Form initializes this manager with the folder path.
        public ImageSequenceManager(string assetsFolder, int maxImages = 9)
        {
            _assetsFolder = assetsFolder;
            _maxImages = maxImages;
            _imageIndex = 1;

            // Handles file system logic inside the library layer.
            if (!Directory.Exists(_assetsFolder))
                Directory.CreateDirectory(_assetsFolder);
        }

        // Returns the full, computed path of the current image file.
        // (OOP: Abstraction) Hides the process of constructing the full file path.
        public string GetCurrentImagePath()
        {
            return Path.Combine(_assetsFolder, $"image{_imageIndex}.png");
        }

        // Moves to the next image in the sequence and returns its path, or null if the sequence is complete.
        // (OOP: Encapsulation & Abstraction) Modifies the private index and encapsulates the end-of-sequence check.
        public string GetNextImagePath()
        {
            _imageIndex++;
            if (_imageIndex > _maxImages)
                return null;

            return Path.Combine(_assetsFolder, $"image{_imageIndex}.png");
        }

        // Checks if there are still more images left to display based on the private index.
        // (OOP: Encapsulation) Provides read-only access to the internal progress state.
        public bool HasMoreImages()
        {
            return _imageIndex < _maxImages;
        }

        // Resets the sequence index back to the first image.
        public void Reset()
        {
            _imageIndex = 1;
        }
    }
}