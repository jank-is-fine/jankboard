using System.Numerics;
using Rendering.UI;

namespace Managers
{
    /// <summary>
    /// <para>Manages imported images with the corresponding UIImage class</para>
    /// <para>Handles image imports, see <see cref="CreateNewImage(Vector2? targetPos, string Path)"/></para>
    /// </summary>

    public static class ImageManager
    {
        private static Dictionary<Guid, (ImageData, ImageUI?)> images = [];
        public static List<ImageData> GetAllImages => [.. images.Select(x => x.Value.Item1).Where(x => !x.IsDeleted)];

        public static void AddImages(List<ImageData> targetImages)
        {
            targetImages.Where(x => x != null).ToList().ForEach(x => images.TryAdd(x.guid, (x, null)));
        }

        public static void LoadFromSave(List<ImageData> TargetImages)
        {
            images.Clear();

            foreach (var g in TargetImages)
            {
                images.TryAdd(g.guid, (g, null));
            }
        }

        public static void LoadImages(Guid target)
        {
            List<ImageData> targetImages = [.. images.Where(x => x.Value.Item1.ParentEntryGuid == target && !x.Value.Item1.IsDeleted).Select(x => x.Value.Item1)];
            CreateNewImageUIs(targetImages);
        }

        public static void CreateNewImage(Vector2? targetPos, string Path)
        {
            var Texture = TextureHandler.GetTextureOrCreateBasedOnPath(Path);
            if (Texture == null) { return; }

            Vector2 spawnPos;
            if (targetPos == null)
            {
                spawnPos = Camera.Position;
            }
            else
            {
                spawnPos = (Vector2)targetPos;
            }

            Guid currentParent = EntryManager.CurrentParentEntry;

            ImageData image = new(currentParent, spawnPos)
            {
                ImagePath = Path,
                Size = new(Texture.Width, Texture.Height)
            };

            images.Add(image.guid, (image, null));
            var newUi = CreateNewImageUI(image);

            var action = new UndoRedoAction(
              undoActions: [() => MarkImagesAsDeleted([image.guid])],
              redoActions: [() => UnmarkImagesAsDeleted([image.guid])]
           );

            UndoRedoManager.ActionExecuted(action);
        }

        public static void CreateNewImages(string[]? Paths, Vector2? TargetPos = null)
        {
            if (Paths == null || Paths.Length < 1) { return; }

            if (TargetPos == null) { TargetPos = Camera.Position; }

            foreach (var path in Paths)
            {
                CreateNewImage(TargetPos, path);
            }
        }

        public static void CreateNewImagesPerWindowDrop(string[] Paths)
        {
            if (Paths == null || Paths.Length < 1) { return; }
            CreateNewImages(Paths);
        }


        public static List<ImageUI> CreateNewImageUIs(List<ImageData>? images)
        {
            if (images == null) { return []; }

            List<ImageUI> createdUIs = [];

            foreach (ImageData image in images)
            {
                var CreatedImageUI = CreateNewImageUI(image);
                if (CreatedImageUI != null)
                {
                    createdUIs.Add(CreatedImageUI);
                }
            }

            return createdUIs;
        }

        public static ImageUI? CreateNewImageUI(ImageData image)
        {
            if (images.TryGetValue(image.guid, out _))
            {
                ImageUI uI = new(image)
                {
                    Transform = { Position = image.position, Scale = image.Size },
                    RenderOrder = 1,
                };
                uI.RecalcHandlerPos();

                ChunkManager.AddObject(uI);

                images[image.guid] = (image, uI);

                return uI;
            }
            return null;
        }

        public static void MarkSelectedImagesAsDeleted()
        {
            List<ImageUI>? affected = SelectionManager.GetSelectedTypeOfObject<ImageUI>();
            if (affected == null || affected.Count < 1) { return; }

            List<Guid> guids = [.. affected.Select(x => x.ReferenceImage.guid)];
            MarkImagesAsDeleted(guids);

            var action = new UndoRedoAction(
                undoActions: [() => UnmarkImagesAsDeleted(guids)],
                redoActions: [() => MarkImagesAsDeleted(guids)],
                PopActions: [() => RemoveImages(guids)]
            );

            UndoRedoManager.ActionExecuted(action);
        }

        public static void MarkImagesAsDeleted(List<Guid> guids)
        {
            foreach (var guid in guids)
            {
                if (images.TryGetValue(guid, out var tuple))
                {
                    tuple.Item1.IsDeleted = true;
                    if (tuple.Item2 != null)
                    {
                        tuple.Item2.IsVisible = false;
                        SelectionManager.Deselect(tuple.Item2);
                    }
                }
            }
        }

        public static void UnmarkImagesAsDeleted(List<Guid> guids)
        {
            foreach (var guid in guids)
            {
                if (images.TryGetValue(guid, out var tuple))
                {
                    tuple.Item1.IsDeleted = false;
                    if (tuple.Item2 != null)
                    {
                        tuple.Item2.IsVisible = true;
                    }
                }
            }
        }

        public static void CleanUpDeletedImages()
        {
            var entriesToRemove = images.Where(x => x.Value.Item1 == null || x.Value.Item1.IsDeleted).Select(x => x.Key).ToList();
            entriesToRemove.ForEach(x => images.Remove(x));
        }

        public static void RemoveImages(List<Guid> guids)
        {
            foreach (var guid in guids)
            {
                images.Remove(guid);
            }
        }

        public static void Dispose()
        {
            foreach (var ui in images.Where(x => x.Value.Item2 != null))
            {
                ui.Value.Item2?.Dispose();
            }
        }
    }
}