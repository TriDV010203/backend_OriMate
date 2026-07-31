using System;
using System.Collections.Generic;

namespace OrigamiPlatform.Domain.Constants; // Hoặc OrigamiPlatform.Application.Constants tùy bạn đặt

public static class AiDictionary
{
    // Viết duy nhất 1 lần ở đây
    public static readonly Dictionary<string, string> KeywordMapping = new(StringComparer.OrdinalIgnoreCase)
     {
        // Chó -> "dog"
        { "Chihuahua", "dog" }, { "Japanese spaniel", "dog" }, { "Maltese dog", "dog" }, { "Pekinese", "dog" },
        { "Shih-Tzu", "dog" }, { "Blenheim spaniel", "dog" }, { "papillon", "dog" }, { "toy terrier", "dog" },
        { "Rhodesian ridgeback", "dog" }, { "Afghan hound", "dog" }, { "basset", "dog" }, { "beagle", "dog" },
        { "bloodhound", "dog" }, { "bluetick", "dog" }, { "black-and-tan coonhound", "dog" }, { "Walker hound", "dog" },
        { "English foxhound", "dog" }, { "redbone", "dog" }, { "borzoi", "dog" }, { "Irish wolfhound", "dog" },
        { "Italian greyhound", "dog" }, { "whippet", "dog" }, { "Ibizan hound", "dog" }, { "Norwegian elkhound", "dog" },
        { "otterhound", "dog" }, { "Saluki", "dog" }, { "Scottish deerhound", "dog" }, { "Weimaraner", "dog" },
        { "Staffordshire bullterrier", "dog" }, { "American Staffordshire terrier", "dog" }, { "Bedlington terrier", "dog" },
        { "Border terrier", "dog" }, { "Kerry blue terrier", "dog" }, { "Irish terrier", "dog" }, { "Norfolk terrier", "dog" },
        { "Norwich terrier", "dog" }, { "Yorkshire terrier", "dog" }, { "wire-haired fox terrier", "dog" },
        { "Lakeland terrier", "dog" }, { "Sealyham terrier", "dog" }, { "Airedale", "dog" }, { "cairn", "dog" },
        { "Australian terrier", "dog" }, { "Dandie Dinmont", "dog" }, { "Boston bull", "dog" }, { "miniature schnauzer", "dog" },
        { "giant schnauzer", "dog" }, { "standard schnauzer", "dog" }, { "Scotch terrier", "dog" }, { "Tibetan terrier", "dog" },
        { "silky terrier", "dog" }, { "soft-coated wheaten terrier", "dog" }, { "West Highland white terrier", "dog" }, { "Lhasa", "dog" },
        { "flat-coated retriever", "dog" }, { "curly-coated retriever", "dog" }, { "golden retriever", "dog" }, { "Labrador retriever", "dog" },
        { "Chesapeake Bay retriever", "dog" }, { "German short-haired pointer", "dog" }, { "vizsla", "dog" }, { "English setter", "dog" },
        { "Irish setter", "dog" }, { "Gordon setter", "dog" }, { "Brittany spaniel", "dog" }, { "clumber", "dog" }, { "English springer", "dog" },
        { "Welsh springer spaniel", "dog" }, { "cocker spaniel", "dog" }, { "Sussex spaniel", "dog" }, { "Irish water spaniel", "dog" },
        { "kuvasz", "dog" }, { "schipperke", "dog" }, { "groenendael", "dog" }, { "malinois", "dog" }, { "briard", "dog" }, { "kelpie", "dog" },
        { "komondor", "dog" }, { "Old English sheepdog", "dog" }, { "Shetland sheepdog", "dog" }, { "collie", "dog" }, { "Border collie", "dog" },
        { "Bouvier des Flandres", "dog" }, { "Rottweiler", "dog" }, { "German shepherd", "dog" }, { "Doberman", "dog" },
        { "miniature pinscher", "dog" }, { "Greater Swiss Mountain dog", "dog" }, { "Bernese mountain dog", "dog" },
        { "Appenzeller", "dog" }, { "EntleBucher", "dog" }, { "boxer", "dog" }, { "bull mastiff", "dog" },
        { "Tibetan mastiff", "dog" }, { "French bulldog", "dog" }, { "Great Dane", "dog" }, { "Saint Bernard", "dog" },
        { "Eskimo dog", "dog" }, { "malamute", "dog" }, { "Siberian husky", "dog" }, { "dalmatian", "dog" }, { "affenpinscher", "dog" },
        { "basenji", "dog" }, { "pug", "dog" }, { "Leonberg", "dog" }, { "Newfoundland", "dog" }, { "Great Pyrenees", "dog" },
        { "Samoyed", "dog" }, { "Pomeranian", "dog" }, { "chow", "dog" }, { "keeshond", "dog" }, { "Brabancon griffon", "dog" },
        { "Pembroke", "dog" }, { "Cardigan", "dog" }, { "toy poodle", "dog" }, { "miniature poodle", "dog" }, { "standard poodle", "dog" }, { "Mexican hairless", "dog" },

        // Mèo -> "cat"
        { "cat", "cat" }, { "tabby", "cat" }, { "siamese", "cat" }, { "kitten", "cat" }, { "persian", "cat" },

        // Chim -> "bird"
        { "bird", "bird" }, { "parrot", "bird" }, { "lorikeet", "bird" }, { "duck", "bird" },
        { "owl", "bird" }, { "eagle", "bird" }, { "swan", "bird" }, { "penguin", "bird" }, { "jay", "bird" },

        // Đồ vật / Rương / Hộp -> "chest" hoặc "box"
        { "chest", "chest" }, { "box", "chest" }, { "trunk", "chest" }, { "crate", "chest" },

        // Thiết bị điện tử -> "tv"
        { "television", "tv" }, { "monitor", "tv" }, { "screen", "tv" }
    };

}