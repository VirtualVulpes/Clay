using System.Diagnostics;
using Armature.DebugUi;
using Armature.Editor;
using Armature.Input;
using Armature.Rendering;
using Armature.Scene;
using Armature.Scene.Systems;
using Armature.SceneRendering;
using Brutal.GlfwApi;
using Brutal.Numerics;
using Clay.Scenes;

namespace Clay;

public sealed class App : IDisposable
{
    private readonly GlfwWindow _window;
    private readonly InputManager _inputManager;
    private readonly GraphicsBackend _graphics;
    
    private readonly AssetLibrary _assets;

    private readonly SceneRenderer _sceneRenderer;
    
    private readonly ImGuiManager _imGuiManager;
    
    private readonly SystemManager _systemManager;
    private readonly CameraSystem _camera;
    private readonly CameraController _cameraController;

    private readonly Editor _editor;

    public App()
    {
        _window = Glfw.CreateWindow(new GlfwWindow.CreateInfo { Title = "Clay", Size = new int2(800, 600) });

        _inputManager = new InputManager(_window);

        var world = new World();

        _systemManager = new SystemManager();
        _systemManager.AddSystem(new RenderSystem(world));
        _systemManager.AddSystem(new RotateSystem(world));
        _camera = _systemManager.AddSystem(new CameraSystem(world));

        _graphics = new GraphicsBackend(_window);

        _imGuiManager = new ImGuiManager(_graphics, _window, _inputManager);
        
        _assets = new AssetLibrary(_graphics, Path.Join(AppContext.BaseDirectory, "textures"), Path.Join(AppContext.BaseDirectory, "models"));
        _assets.LoadTextures("brick.ktx2", "statue.ktx2", "viking_room.ktx2");
        _assets.LoadModel("viking_room", "viking_room.gltf");
        
        _sceneRenderer = new SceneRenderer(_graphics, _assets.Meshes);

        var resolver = new AssetResolver(_assets, _graphics.Pipelines);
        new SceneLoader(resolver).Load(DemoScene.Create(), world);

        _cameraController = new CameraController(world, _inputManager, _camera);
        _editor = new Editor(_graphics, _inputManager, world);
    }

    public void Run()
    {
        _inputManager.SetInputMode(InputMode.CameraControl);

        _systemManager.Start();

        var last = Stopwatch.GetTimestamp();

        var renderSystem = _systemManager.GetSystem<RenderSystem>();

        while (!_window.ShouldClose)
        {
            var now = Stopwatch.GetTimestamp();
            var deltaTime = float.Min((float)Stopwatch.GetElapsedTime(last, now).TotalSeconds, 0.1f);
            last = now;

            _inputManager.Update();

            Glfw.PollEvents();

            _systemManager.Update(deltaTime);
            _cameraController.Update(deltaTime);

            if (_inputManager.IsKeyPressed(GlfwKey.Escape) && !_inputManager.IsUiCapturingKeyboard())
                _window.ShouldClose = true;

            _imGuiManager.NewFrame(deltaTime);

            _editor.Update();

            renderSystem.BuildScene(_camera.GetActiveCameraData(_graphics.Size));

            _sceneRenderer.Submit(renderSystem.Scene);
            _graphics.Renderer.DrawFrame(_sceneRenderer.Items, _sceneRenderer.ViewProj);
        }
    }

    public void Dispose()
    {
        _graphics.WaitIdle();

        _editor.Dispose();
        _imGuiManager.Dispose();
        _sceneRenderer.Dispose();
        _assets.Dispose();
        _inputManager.Dispose();
        _graphics.Dispose();
        _window.Dispose();

        Glfw.Terminate();
    }
}