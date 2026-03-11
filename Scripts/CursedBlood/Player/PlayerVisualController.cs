using System;
using System.Collections.Generic;
using CursedBlood.Core;
using Godot;

namespace CursedBlood.Player
{
    public partial class PlayerVisualController : Node2D
    {
        private const string BodySpriteNodeName = "BodySprite";
        private const string DrillSpriteNodeName = "DrillSprite";
        private const string DownAnimation = "down";
        private const string DownLeftAnimation = "down_left";
        private const string DownRightAnimation = "down_right";
        private const string LeftAnimation = "left";
        private const string RightAnimation = "right";
        private const string UpAnimation = "up";
        private const string UpLeftAnimation = "up_left";
        private const string UpRightAnimation = "up_right";
        private const int FrameSize = 128;
        private const int FramesPerAxis = 3;

        private static readonly string[] AnimationNames =
        {
            DownAnimation,
            DownLeftAnimation,
            DownRightAnimation,
            LeftAnimation,
            RightAnimation,
            UpAnimation,
            UpLeftAnimation,
            UpRightAnimation
        };

        private static readonly Dictionary<Vector2I, string> DirectionToAnimation = new()
        {
            [new Vector2I(0, 1)] = DownAnimation,
            [new Vector2I(-1, 1)] = DownLeftAnimation,
            [new Vector2I(1, 1)] = DownRightAnimation,
            [new Vector2I(-1, 0)] = LeftAnimation,
            [new Vector2I(1, 0)] = RightAnimation,
            [new Vector2I(0, -1)] = UpAnimation,
            [new Vector2I(-1, -1)] = UpLeftAnimation,
            [new Vector2I(1, -1)] = UpRightAnimation
        };

        private static readonly Dictionary<string, string> DrillDirectionToFileToken = new()
        {
            [DownAnimation] = "down",
            [DownLeftAnimation] = "down_left",
            [DownRightAnimation] = "down_right",
            [LeftAnimation] = "left",
            [RightAnimation] = "right",
            [UpAnimation] = "up",
            [UpLeftAnimation] = "top_left",
            [UpRightAnimation] = "top_right"
        };

        private static readonly string[] BodyPathTemplates =
        {
            "res://Assets/Sprites/Player/Female/player_female_body_{0}.png",
            "res://Asset/Sprites/Player/Female/player_female_body_{0}.png"
        };

        private static readonly string[] DrillPathTemplates =
        {
            "res://Assets/Sprites/Equipment/Drill/drill_machine_{0}.png",
            "res://Asset/Sprites/Equipment/Drill/drill_machine_{0}.png"
        };

        private readonly Dictionary<string, Vector2> _drillOffsets = new();
        private readonly Dictionary<string, Vector2> _bodyFrameSizes = new();
        private readonly Dictionary<string, Vector2> _drillFrameSizes = new();
        private AnimatedSprite2D _bodySprite;
        private AnimatedSprite2D _drillSprite;
        private Texture2D _bodyFallbackSheet;
        private Texture2D _drillFallbackSheet;
        private bool _initialized;

        [Export]
        public double AnimationFps { get; set; } = 10.0;

        [Export]
        public bool EnableDebugLogs { get; set; } = true;

        [Export]
        public Vector2 DownDrillOffset { get; set; } = new Vector2(0f, 10f);

        [Export]
        public Vector2 DownLeftDrillOffset { get; set; } = new Vector2(-8f, 8f);

        [Export]
        public Vector2 DownRightDrillOffset { get; set; } = new Vector2(8f, 8f);

        [Export]
        public Vector2 LeftDrillOffset { get; set; } = new Vector2(-10f, 0f);

        [Export]
        public Vector2 RightDrillOffset { get; set; } = new Vector2(10f, 0f);

        [Export]
        public Vector2 UpDrillOffset { get; set; } = new Vector2(0f, -8f);

        [Export]
        public Vector2 UpLeftDrillOffset { get; set; } = new Vector2(-8f, -8f);

        [Export]
        public Vector2 UpRightDrillOffset { get; set; } = new Vector2(8f, -8f);

        public string CurrentDirectionName { get; private set; } = DownAnimation;

        public bool HasMissingAssets { get; private set; }

        public IReadOnlyDictionary<string, Vector2> DrillOffsets => _drillOffsets;

        public override void _Ready()
        {
            EnsureInitialized();
        }

        public void UpdateVisual(Vector2I direction, bool isMoving, int playerSize)
        {
            EnsureInitialized();

            var animationName = ResolveAnimationName(direction);
            if (animationName != CurrentDirectionName && EnableDebugLogs)
            {
                GD.Print($"[PlayerVisual] direction={animationName}");
            }

            CurrentDirectionName = animationName;
            ApplyScale(Math.Max(1, playerSize), animationName);
            ApplySpriteAnimation(_bodySprite, animationName, isMoving);
            ApplySpriteAnimation(_drillSprite, animationName, isMoving);
            _drillSprite.Position = GetDrillOffset(animationName);
        }

        public void RefreshOffsets()
        {
            EnsureInitialized();
            BuildOffsetMap();
            _drillSprite.Position = GetDrillOffset(CurrentDirectionName);
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            HasMissingAssets = false;
            _bodySprite = GetNodeOrNull<AnimatedSprite2D>(BodySpriteNodeName) ?? CreateSpriteNode(BodySpriteNodeName, 0);
            _drillSprite = GetNodeOrNull<AnimatedSprite2D>(DrillSpriteNodeName) ?? CreateSpriteNode(DrillSpriteNodeName, 1);

            _bodySprite.SpriteFrames = BuildSpriteFrames(isBody: true, _bodyFrameSizes);
            _drillSprite.SpriteFrames = BuildSpriteFrames(isBody: false, _drillFrameSizes);

            BuildOffsetMap();
            ApplyScale(5, DownAnimation);
            ApplySpriteAnimation(_bodySprite, DownAnimation, false);
            ApplySpriteAnimation(_drillSprite, DownAnimation, false);
            _drillSprite.Position = GetDrillOffset(DownAnimation);
            _initialized = true;
        }

        private AnimatedSprite2D CreateSpriteNode(string nodeName, int zIndex)
        {
            var sprite = new AnimatedSprite2D
            {
                Name = nodeName,
                Centered = true,
                ZIndex = zIndex,
                TextureFilter = TextureFilterEnum.Nearest
            };

            AddChild(sprite);
            return sprite;
        }

        private SpriteFrames BuildSpriteFrames(bool isBody, Dictionary<string, Vector2> frameSizes)
        {
            var frames = new SpriteFrames();
            frameSizes.Clear();
            for (var index = 0; index < AnimationNames.Length; index++)
            {
                var animationName = AnimationNames[index];
                frames.AddAnimation(animationName);
                frames.SetAnimationLoop(animationName, true);
                frames.SetAnimationSpeed(animationName, AnimationFps);

                var texture = LoadSpriteTexture(isBody, animationName);
                frameSizes[animationName] = AddFrames(frames, animationName, texture);
            }

            return frames;
        }

        private Texture2D LoadSpriteTexture(bool isBody, string animationName)
        {
            var fileToken = isBody ? animationName : DrillDirectionToFileToken[animationName];
            var templates = isBody ? BodyPathTemplates : DrillPathTemplates;
            var attemptedPaths = new List<string>(templates.Length * (isBody ? 2 : 1));

            for (var index = 0; index < templates.Length; index++)
            {
                var tokenVariants = isBody
                    ? new[] { fileToken, string.Concat(fileToken, "_") }
                    : new[] { fileToken };
                for (var variantIndex = 0; variantIndex < tokenVariants.Length; variantIndex++)
                {
                    var candidatePath = templates[index].Replace("{0}", tokenVariants[variantIndex], StringComparison.Ordinal);
                    if (!attemptedPaths.Contains(candidatePath))
                    {
                        attemptedPaths.Add(candidatePath);
                    }

                    if (!ResourceLoader.Exists(candidatePath))
                    {
                        continue;
                    }

                    var texture = ResourceLoader.Load<Texture2D>(candidatePath);
                    if (texture != null)
                    {
                        return texture;
                    }

                    GD.PushError($"Failed to load {(isBody ? "body" : "drill")} sprite texture: {candidatePath}");
                }
            }

            HasMissingAssets = true;
            GD.PushError($"Missing {(isBody ? "body" : "drill")} sprite texture for '{animationName}'. Checked: {string.Join(", ", attemptedPaths)}");
            return GetFallbackSheet(isBody);
        }

        private static Vector2 AddFrames(SpriteFrames frames, string animationName, Texture2D texture)
        {
            if (IsLegacySpriteSheet(texture))
            {
                for (var row = 0; row < FramesPerAxis; row++)
                {
                    for (var col = 0; col < FramesPerAxis; col++)
                    {
                        var atlasTexture = new AtlasTexture
                        {
                            Atlas = texture,
                            Region = new Rect2(col * FrameSize, row * FrameSize, FrameSize, FrameSize)
                        };
                        frames.AddFrame(animationName, atlasTexture);
                    }
                }

                return new Vector2(FrameSize, FrameSize);
            }

            frames.AddFrame(animationName, texture);
            return new Vector2(texture.GetWidth(), texture.GetHeight());
        }

        private static bool IsLegacySpriteSheet(Texture2D texture)
        {
            return texture != null && texture.GetWidth() == FrameSize * FramesPerAxis && texture.GetHeight() == FrameSize * FramesPerAxis;
        }

        private Texture2D GetFallbackSheet(bool isBody)
        {
            if (isBody)
            {
                _bodyFallbackSheet ??= CreateFallbackSheet(new Color(0.85f, 0.15f, 0.65f, 0.85f));
                return _bodyFallbackSheet;
            }

            _drillFallbackSheet ??= CreateFallbackSheet(new Color(0.95f, 0.82f, 0.18f, 0.85f));
            return _drillFallbackSheet;
        }

        private static Texture2D CreateFallbackSheet(Color color)
        {
            var image = Image.CreateEmpty(FrameSize * FramesPerAxis, FrameSize * FramesPerAxis, false, Image.Format.Rgba8);
            image.Fill(color);
            return ImageTexture.CreateFromImage(image);
        }

        private void BuildOffsetMap()
        {
            _drillOffsets.Clear();
            _drillOffsets[DownAnimation] = DownDrillOffset;
            _drillOffsets[DownLeftAnimation] = DownLeftDrillOffset;
            _drillOffsets[DownRightAnimation] = DownRightDrillOffset;
            _drillOffsets[LeftAnimation] = LeftDrillOffset;
            _drillOffsets[RightAnimation] = RightDrillOffset;
            _drillOffsets[UpAnimation] = UpDrillOffset;
            _drillOffsets[UpLeftAnimation] = UpLeftDrillOffset;
            _drillOffsets[UpRightAnimation] = UpRightDrillOffset;
        }

        private void ApplyScale(int playerSize, string animationName)
        {
            var worldSize = playerSize * ChunkManager.CellSize;
            _bodySprite.Scale = ResolveScale(_bodyFrameSizes, animationName, worldSize);
            _drillSprite.Scale = ResolveScale(_drillFrameSizes, animationName, worldSize);
        }

        private static Vector2 ResolveScale(IReadOnlyDictionary<string, Vector2> frameSizes, string animationName, float worldSize)
        {
            var frameSize = frameSizes.TryGetValue(animationName, out var size)
                ? size
                : new Vector2(FrameSize, FrameSize);
            var referenceSize = Mathf.Max(1f, Mathf.Max(frameSize.X, frameSize.Y));
            var scale = worldSize / referenceSize;
            return new Vector2(scale, scale);
        }

        private static void ApplySpriteAnimation(AnimatedSprite2D sprite, string animationName, bool isMoving)
        {
            if (sprite == null)
            {
                return;
            }

            var animationChanged = sprite.Animation != animationName;
            if (animationChanged)
            {
                sprite.Play(animationName);
                if (!isMoving)
                {
                    sprite.Frame = 0;
                    sprite.Pause();
                }

                return;
            }

            if (isMoving)
            {
                if (!sprite.IsPlaying())
                {
                    sprite.Play();
                }

                return;
            }

            if (sprite.IsPlaying())
            {
                sprite.Pause();
            }
        }

        private Vector2 GetDrillOffset(string animationName)
        {
            return _drillOffsets.TryGetValue(animationName, out var offset) ? offset : Vector2.Zero;
        }

        private string ResolveAnimationName(Vector2I direction)
        {
            if (direction == Vector2I.Zero)
            {
                return CurrentDirectionName;
            }

            return DirectionToAnimation.TryGetValue(direction, out var animationName)
                ? animationName
                : CurrentDirectionName;
        }
    }
}