using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.U2D.Animation
{
    internal static class SpriteCacheExtensions
    {
        public static MeshCache GetMesh(this SpriteCache sprite)
        {
            if (sprite != null)
                return sprite.skinningCache.GetMesh(sprite);
            return null;
        }

        public static MeshPreviewCache GetMeshPreview(this SpriteCache sprite)
        {
            if (sprite != null)
                return sprite.skinningCache.GetMeshPreview(sprite);
            return null;
        }

        public static SkeletonCache GetSkeleton(this SpriteCache sprite)
        {
            if (sprite != null)
                return sprite.skinningCache.GetSkeleton(sprite);
            return null;
        }

        public static CharacterPartCache GetCharacterPart(this SpriteCache sprite)
        {
            if (sprite != null)
                return sprite.skinningCache.GetCharacterPart(sprite);
            return null;
        }

        public static bool IsVisible(this SpriteCache sprite)
        {
            bool isVisible = true;
            CharacterPartCache characterPart = sprite.GetCharacterPart();

            if (sprite.skinningCache.mode == SkinningMode.Character && characterPart != null)
                isVisible = characterPart.isVisible;

            return isVisible;
        }

        public static Matrix4x4 GetLocalToWorldMatrixFromMode(this SpriteCache sprite)
        {
            SkinningCache skinningCache = sprite.skinningCache;

            if (skinningCache.mode == SkinningMode.SpriteSheet)
                return sprite.localToWorldMatrix;

            CharacterPartCache characterPart = sprite.GetCharacterPart();

            Debug.Assert(characterPart != null);

            return characterPart.localToWorldMatrix;
        }

        public static BoneCache[] GetBonesFromMode(this SpriteCache sprite)
        {
            SkinningCache skinningCache = sprite.skinningCache;

            if (skinningCache.mode == SkinningMode.SpriteSheet)
                return sprite.GetSkeleton().bones;

            CharacterPartCache characterPart = sprite.GetCharacterPart();
            Debug.Assert(characterPart != null);
            return characterPart.bones;
        }

        public static void UpdateMesh(this SpriteCache sprite, BoneCache[] bones)
        {
            MeshCache mesh = sprite.GetMesh();
            MeshPreviewCache previewMesh = sprite.GetMeshPreview();

            Debug.Assert(mesh != null);
            Debug.Assert(previewMesh != null);

            mesh.bones = bones;

            previewMesh.SetWeightsDirty();
        }

        public static void SmoothFill(this SpriteCache sprite)
        {
            MeshCache mesh = sprite.GetMesh();

            if (mesh == null)
                return;

            SpriteMeshDataController controller = new SpriteMeshDataController();
            controller.spriteMeshData = mesh;
            controller.SmoothFill();
        }

        public static void RestoreBindPose(this SpriteCache sprite)
        {
            SkinningCache skinningCache = sprite.skinningCache;
            SkeletonCache skeleton = sprite.GetSkeleton();
            Debug.Assert(skeleton != null);
            skeleton.RestoreDefaultPose();
            skinningCache.events.skeletonPreviewPoseChanged.Invoke(skeleton);
        }

        public static bool AssociateAllBones(this SpriteCache sprite)
        {
            SkinningCache skinningCache = sprite.skinningCache;

            if (skinningCache.mode == SkinningMode.SpriteSheet)
                return false;

            CharacterCache character = skinningCache.character;
            Debug.Assert(character != null);
            Debug.Assert(character.skeleton != null);

            CharacterPartCache characterPart = sprite.GetCharacterPart();

            Debug.Assert(characterPart != null);

            BoneCache[] bones = character.skeleton.bones.Where(x => x.isVisible).ToArray();
            characterPart.bones = bones;

            characterPart.sprite.UpdateMesh(bones);

            return true;
        }

        public static bool AssociatePossibleBones(this SpriteCache sprite)
        {
            SkinningCache skinningCache = sprite.skinningCache;

            if (skinningCache.mode == SkinningMode.SpriteSheet)
                return false;

            CharacterCache character = skinningCache.character;
            Debug.Assert(character != null);
            Debug.Assert(character.skeleton != null);

            CharacterPartCache characterPart = sprite.GetCharacterPart();

            Debug.Assert(characterPart != null);

            BoneCache[] bones = character.skeleton.bones.Where(x => x.isVisible).ToArray();
            List<BoneCache> possibleBones = new List<BoneCache>();
            // check if any of the bones overlapped
            BoneCache shortestBoneDistance = null;
            float minDistances = float.MaxValue;
            Rect characterSpriteRect = new Rect(characterPart.position.x, characterPart.position.y, characterPart.sprite.textureRect.width, characterPart.sprite.textureRect.height);
            foreach (BoneCache bone in bones)
            {
                Vector3 startPoint = bone.position;
                Vector3 endPoint = bone.endPosition;
                if (IntersectsSegment(characterSpriteRect, startPoint, endPoint))
                    possibleBones.Add(bone);
                if (possibleBones.Count == 0)
                {
                    // compare bone start end with rect's 4 line
                    // compare rect point with bone line
                    Vector2[] points = new Vector2[] { startPoint, endPoint };
                    Vector2Int[] rectLinePoints = new[]
                    {
                        new Vector2Int(0, 1),
                        new Vector2Int(0, 2),
                        new Vector2Int(1, 3),
                        new Vector2Int(2, 3),
                    };
                    Vector2[] rectPoints = new[]
                    {
                        new Vector2(characterSpriteRect.xMin, characterSpriteRect.yMin),
                        new Vector2(characterSpriteRect.xMin, characterSpriteRect.yMax),
                        new Vector2(characterSpriteRect.xMax, characterSpriteRect.yMin),
                        new Vector2(characterSpriteRect.xMax, characterSpriteRect.yMax)
                    };
                    foreach (Vector2 point in points)
                    {
                        foreach (Vector2Int rectLine in rectLinePoints)
                        {
                            float distance = PointToLineSegmentDistance(point, rectPoints[rectLine.x], rectPoints[rectLine.y]);
                            if (distance < minDistances)
                            {
                                minDistances = distance;
                                shortestBoneDistance = bone;
                            }
                        }
                    }

                    foreach (Vector2 rectPoint in rectPoints)
                    {
                        float distance = PointToLineSegmentDistance(rectPoint, startPoint, endPoint);
                        if (distance < minDistances)
                        {
                            minDistances = distance;
                            shortestBoneDistance = bone;
                        }
                    }
                }
            }
            // if none overlapped, we use the bone that is closest to us
            if (possibleBones.Count == 0 && shortestBoneDistance != null)
            {
                possibleBones.Add(shortestBoneDistance);
            }
            characterPart.bones = possibleBones.ToArray();

            characterPart.sprite.UpdateMesh(possibleBones.ToArray());

            return true;
        }

        static float PointToLineSegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 n = b - a;
            Vector2 pa = a - p;

            float c = Vector2.Dot(n, pa);

            // Closest point is a
            if (c > 0.0f)
                return Vector2.Dot(pa, pa);

            Vector2 bp = p - b;

            // Closest point is b
            if (Vector2.Dot(n, bp) > 0.0f)
                return Vector2.Dot(bp, bp);

            // Closest point is between a and b
            Vector2 e = pa - n * (c / Vector2.Dot(n, n));
            return Vector2.Dot(e, e);
        }

        static bool IntersectsSegment(Rect rect, Vector2 p1, Vector2 p2)
        {
            float minX = Mathf.Min(p1.x, p2.x);
            float maxX = Mathf.Max(p1.x, p2.x);

            if (maxX > rect.xMax)
            {
                maxX = rect.xMax;
            }

            if (minX < rect.xMin)
            {
                minX = rect.xMin;
            }

            if (minX > maxX)
            {
                return false;
            }

            float minY = Mathf.Min(p1.y, p2.y);
            float maxY = Mathf.Max(p1.y, p2.y);

            float dx = p2.x - p1.x;

            if (Mathf.Abs(dx) > float.Epsilon)
            {
                float a = (p2.y - p1.y) / dx;
                float b = p1.y - a * p1.x;
                minY = a * minX + b;
                maxY = a * maxX + b;
            }

            if (minY > maxY)
            {
                float tmp = maxY;
                maxY = minY;
                minY = tmp;
            }

            if (maxY > rect.yMax)
            {
                maxY = rect.yMax;
            }

            if (minY < rect.yMin)
            {
                minY = rect.yMin;
            }

            if (minY > maxY)
            {
                return false;
            }

            return true;
        }

        public static void DeassociateUnusedBones(this SpriteCache sprite)
        {
            SkinningCache skinningCache = sprite.skinningCache;

            Debug.Assert(skinningCache.mode == SkinningMode.Character);

            CharacterPartCache characterPart = sprite.GetCharacterPart();

            Debug.Assert(characterPart != null);

            characterPart.DissociateUnusedBones();
        }

        public static void DeassociateAllBones(this SpriteCache sprite)
        {
            SkinningCache skinningCache = sprite.skinningCache;

            if (skinningCache.mode == SkinningMode.SpriteSheet)
                return;

            CharacterPartCache part = sprite.GetCharacterPart();
            if (part.bones.Length == 0)
                return;

            Debug.Assert(part.sprite != null);

            part.bones = new BoneCache[0];
            part.sprite.UpdateMesh(part.bones);

            skinningCache.events.characterPartChanged.Invoke(part);
        }
    }
}
