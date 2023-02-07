﻿using System.Numerics;
using Glade2d;
using Glade2d.Graphics.Layers;
using Glade2d.Input;
using Glade2d.Screens;
using Glade2d.Services;
using GladePlatformer.Shared.Entities;
using Meadow.Foundation;
using Meadow.Foundation.Graphics;

namespace GladePlatformer.Shared.Screens;

public class LevelScreen : Screen, IDisposable
{
    private const float PlayerSpeed = 30;
    private const float TreeSpeedMultiplier = 0.5f;
    private const float MountainSpeedMultiplier = 0.2f;
    private const float Gravity = 10f;
    private const float JumpAcceleration = -50f;
    
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private readonly Layer _skyLayer;
    private readonly Layer _treeLayer;
    private readonly Layer _mountainLayer;
    private readonly Color _backgroundColor = new(57, 120, 168);
    private readonly Player _player;
    private readonly LevelHandler _levelHandler;
    private float _playerPositionX; // Where the player is considered to be in relation to the level
    private float _playerVelocityX; // Velocity to affect fake position, not screen position

    public LevelScreen()
    {
        // set screen dimensions for easy reference
        _screenWidth = GameService.Instance.GameInstance.Renderer.Width;
        _screenHeight = GameService.Instance.GameInstance.Renderer.Height;
        
        // Set background color
        GameService.Instance.GameInstance.Renderer.BackgroundColor = _backgroundColor;

        _skyLayer = CreateSkyLayer();
        _treeLayer = CreateTreeLayer();
        _mountainLayer = CreateMountainLayer();
        
        CreateSun();

        var levelData = new LevelHandler.LevelData(new byte[] { 1, 2, 3, 2, 1 });
        _levelHandler = new LevelHandler(levelData);

        _player = new Player
        {
            X = _screenWidth / 2.0f
        };
        
        _player.Y = _screenHeight - new GroundChunk().CurrentFrame.Height - _player.CurrentFrame.Height;
        AddSprite(_player);
    }

    public override void Activity()
    {
        var frameDelta = (float) GameService.Instance.Time.FrameDelta;
        var lastPositionX = _playerPositionX;
        _playerPositionX += _playerVelocityX * frameDelta;
        _playerVelocityX = 0;

        var playerPositionY = _player.Y;
        var playerVelocityY = _player.VelocityY;
        
        _levelHandler.Update(ref _playerPositionX, ref playerPositionY, ref playerVelocityY);
        _player.Y = playerPositionY;
        _player.VelocityY = playerVelocityY;

        var movedBy = _playerPositionX - lastPositionX;
        _treeLayer.Shift(new Vector2(-movedBy * TreeSpeedMultiplier, 0));
        _mountainLayer.Shift(new Vector2(-movedBy * MountainSpeedMultiplier, 0));
        
        var inputManager = GameService.Instance.GameInstance.InputManager;
        if (inputManager.GetButtonState(GameConstants.InputNames.Right) == ButtonState.Down)
        {
            _playerVelocityX = PlayerSpeed;
        }
        else if (inputManager.GetButtonState(GameConstants.InputNames.Left) == ButtonState.Down)
        {
            _playerVelocityX = -PlayerSpeed;
        }
        
        _player.VelocityY += Gravity;
        if (inputManager.GetButtonState(GameConstants.InputNames.Up) == ButtonState.Pressed)
        {
            _player.VelocityY += JumpAcceleration;
        }
    }

    private Layer CreateTreeLayer()
    {
        var tree = new Tree();
        var ground = new GroundChunk();
        
        // We want to make sure the layer is at least as wide as the screen, 
        // but also wide enough that it can tile with itself, so no seams show
        // when it scrolls. The trees will be staggered in two "depths", with
        // every other in front of the two surrounding to it.
        var layerWidth = _screenWidth +
                         (_screenWidth % (tree.CurrentFrame.Width / 2));
        
        var layer = Layer.Create(new Dimensions(layerWidth, tree.CurrentFrame.Height));
        layer.CameraOffset = new Point( 0, _screenHeight - tree.CurrentFrame.Height - ground.CurrentFrame.Height);
        layer.BackgroundColor = new Color(79, 84, 107);
        layer.Clear();
        
        GameService.Instance.GameInstance.LayerManager.AddLayer(layer, -2);
        
        // TODO: Clear layer to the main color of mountains, to pretend it has
        // transparency.
        
        // Draw background trees first
        for (var x = 0; x < layerWidth; x += tree.CurrentFrame.Width)
        {
            layer.DrawTexture(tree.CurrentFrame, new Point(x, 0));
        }
        
        // Now draw the foreground trees
        for (var x = tree.CurrentFrame.Width / 2; x < layerWidth; x += tree.CurrentFrame.Width)
        {
            layer.DrawTexture(tree.CurrentFrame, new Point(x, 0));
        }

        return layer;
    }

    private Layer CreateMountainLayer()
    {
        var mountain = new MountainChunk();
        
        // We want to make sure the layer is at least as wide as the screen, 
        // but also wide enough that it can tile with itself, so no seams show
        // when it scrolls. 
        var layerWidth = _screenWidth + (_screenWidth % (mountain.CurrentFrame.Width));
        
        var layer = Layer.Create(new Dimensions(layerWidth, mountain.CurrentFrame.Height));
        layer.BackgroundColor = _backgroundColor;
        layer.Clear();
        layer.CameraOffset = new Point( 0, _screenHeight - 16 - mountain.CurrentFrame.Height);
        
        GameService.Instance.GameInstance.LayerManager.AddLayer(layer, -3);

        for (var x = 0; x < layerWidth; x += mountain.CurrentFrame.Width)
        {
            layer.DrawTexture(mountain.CurrentFrame, new Point(x, 0));
        }

        return layer;
    }

    private Layer CreateSkyLayer()
    {
        var skyChunk = new SkyChunk();
        
        // Sky doesn't move, so it can be the same width of the screen
        var layer = Layer.Create(new Dimensions(_screenWidth, skyChunk.CurrentFrame.Height));
        layer.CameraOffset = new Point(0, 0);
        
        GameService.Instance.GameInstance.LayerManager.AddLayer(layer, -1);
        for (var x = 0; x < _screenWidth; x += skyChunk.CurrentFrame.Width)
        {
            layer.DrawTexture(skyChunk.CurrentFrame, new Point(x, 0));
        }

        return layer;
    }

    /// <summary>
    /// Add a sun in the top left of the screen
    /// </summary>
    private void CreateSun()
    {
        var sun = new Sun(_screenWidth - 8 - Sun.ChunkWidth, 8)
        {
            Layer = 0
        };
        
        AddSprite(sun);
    }

    public void Dispose()
    {
        var layerManager = GameService.Instance.GameInstance.LayerManager;
        layerManager.RemoveLayer(_skyLayer);
        layerManager.RemoveLayer(_treeLayer);
        layerManager.RemoveLayer(_mountainLayer);
    }
}