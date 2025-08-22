using System;
using System.Collections.Generic;

namespace ImageProcessingMonolith
{
    //Simulation part
    public class ImageData
    {
        public string Name { get; }
        public List<string> AppliedEffectsDescriptions { get; private set; }

        public ImageData(string name)
        {
            Name = name;
            AppliedEffectsDescriptions = new List<string> { $"Original image '{Name}'" };
        }

        public void AddEffectDescription(string description)
        {
            AppliedEffectsDescriptions.Add(description);
        }

        public override string ToString()
        {
            return string.Join(" -> ", AppliedEffectsDescriptions);
        }
    }

    //wrapper for Effects
    public class EffectParameter
    {
        public string ParameterName { get; }
        public object Value { get; }

        public EffectParameter(string parameterName, object value)
        {
            ParameterName = parameterName;
            Value = value;
        }

        public override string ToString()
        {
            return $"{ParameterName}: {Value}";
        }
    }

    public interface IImageEffect
    {
        string Name { get; }
        bool ApplyEffect(ImageData image, EffectParameter parameter = null);
    }

    //Resize effect plugin
    public class ResizeEffect : IImageEffect
    {
        public string Name => "Resize";

        public bool ApplyEffect(ImageData image, EffectParameter parameter = null)
        {
            if (parameter == null || !(parameter.Value is int))
                throw new ArgumentException("ResizeEffect requires an integer parameter for target size.");

            int newSize = (int)parameter.Value;
            image.AddEffectDescription($"Resize to {newSize}px");
            return true;
        }
    }

    //Blur effect plugin
    public class BlurEffect : IImageEffect
    {
        public string Name => "Blur";

        public bool ApplyEffect(ImageData image, EffectParameter parameter = null)
        {
            if (parameter == null || !(parameter.Value is int))
                throw new ArgumentException("BlurEffect requires an integer parameter for blur size.");

            int blurSize = (int)parameter.Value;
            image.AddEffectDescription($"Blur {blurSize}px");
            return true;
        }
    }

    //Grayscale effect plugin
    public class GrayscaleEffect : IImageEffect
    {
        public string Name => "Grayscale";

        public bool ApplyEffect(ImageData image, EffectParameter parameter = null)
        {
            image.AddEffectDescription("Convert to Grayscale");
            return true;
        }
    }

    //creation and registration plugin
    public class EffectFactory
    {
        private Dictionary<string, Func<IImageEffect>> effectRegistry = new Dictionary<string, Func<IImageEffect>>();

        public EffectFactory()
        {
            RegisterPlugin("Resize", () => new ResizeEffect());
            RegisterPlugin("Blur", () => new BlurEffect());
            RegisterPlugin("Grayscale", () => new GrayscaleEffect());
        }

        public void RegisterPlugin(string name, Func<IImageEffect> constructor)
        {
            if (!effectRegistry.ContainsKey(name))
                effectRegistry.Add(name, constructor);
        }

        public void UnregisterPlugin(string name)
        {
            if (effectRegistry.ContainsKey(name))
                effectRegistry.Remove(name);
        }

        public IImageEffect CreateEffect(string name)
        {
            if (effectRegistry.ContainsKey(name))
                return effectRegistry[name]();
            throw new ArgumentException($"Effect '{name}' is not registered.");
        }

        public IEnumerable<string> GetAvailableEffectNames()
        {
            return effectRegistry.Keys;
        }
    }

    public class ImageEffectRequest
    {
        public string EffectName { get; }
        public EffectParameter Parameter { get; }

        public ImageEffectRequest(string effectName, EffectParameter parameter = null)
        {
            EffectName = effectName;
            Parameter = parameter;
        }
    }

    public class ImageProcessor
    {
        private EffectFactory effectFactory = new EffectFactory();
        private Dictionary<ImageData, List<ImageEffectRequest>> imagesAndEffects = new Dictionary<ImageData, List<ImageEffectRequest>>();

        public void AddImage(ImageData image)
        {
            if (!imagesAndEffects.ContainsKey(image))
                imagesAndEffects[image] = new List<ImageEffectRequest>();
        }

        public void RemoveImage(ImageData image)
        {
            if (imagesAndEffects.ContainsKey(image))
                imagesAndEffects.Remove(image);
        }

        public void AddEffectToImage(ImageData image, ImageEffectRequest effectRequest)
        {
            if (!imagesAndEffects.ContainsKey(image))
                throw new ArgumentException("Image not registered for processing.");

            imagesAndEffects[image].Add(effectRequest);
        }

        public void RemoveEffectFromImage(ImageData image, string effectName)
        {
            if (!imagesAndEffects.ContainsKey(image))
                return;

            var effects = imagesAndEffects[image];
            effects.RemoveAll(e => e.EffectName == effectName);
        }

        public void ProcessAll()
        {
            foreach (var kvp in imagesAndEffects)
            {
                ImageData image = kvp.Key;
                List<ImageEffectRequest> effects = kvp.Value;

                foreach (var effectRequest in effects)
                {
                    try
                    {
                        IImageEffect effectPlugin = effectFactory.CreateEffect(effectRequest.EffectName);
                        effectPlugin.ApplyEffect(image, effectRequest.Parameter);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to apply effect '{effectRequest.EffectName}' on image '{image.Name}': {ex.Message}");
                    }
                }
            }
        }

        public EffectFactory GetEffectFactory() => effectFactory;

        public IEnumerable<ImageData> GetAllImages() => imagesAndEffects.Keys;
    }

    class Program
    {
        static void Main()
        {
            //testing all parts like this
            ImageProcessor processor = new ImageProcessor();

            ImageData img1 = new ImageData("Image#1");
            ImageData img2 = new ImageData("Image#2");
            ImageData img3 = new ImageData("Image#3");

            processor.AddImage(img1);
            processor.AddImage(img2);
            processor.AddImage(img3);

            processor.AddEffectToImage(img1, new ImageEffectRequest("Resize", new EffectParameter("Size", 100)));
            processor.AddEffectToImage(img1, new ImageEffectRequest("Blur", new EffectParameter("Size", 2)));

            processor.AddEffectToImage(img2, new ImageEffectRequest("Resize", new EffectParameter("Size", 100)));

            processor.AddEffectToImage(img3, new ImageEffectRequest("Resize", new EffectParameter("Size", 150)));
            processor.AddEffectToImage(img3, new ImageEffectRequest("Blur", new EffectParameter("Size", 5)));
            processor.AddEffectToImage(img3, new ImageEffectRequest("Grayscale"));

            processor.ProcessAll();

            foreach (var image in processor.GetAllImages())
            {
                Console.WriteLine($"Processed {image.Name}: {image}");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
