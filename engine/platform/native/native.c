/*
 * PyFlare Engine - Native Platform Layer
 * Low-level window management, OpenGL context, and platform abstraction
 * Optimized for old hardware (OpenGL 2.1+ support)
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdbool.h>

#ifdef _WIN32
    #define WIN32_LEAN_AND_MEAN
    #include <windows.h>
    #include <GL/gl.h>
    #pragma comment(lib, "opengl32.lib")
#elif __APPLE__
    #include <OpenGL/gl.h>
    #include <OpenGL/glu.h>
    #include <GLUT/glut.h>
#else
    #include <GL/gl.h>
    #include <GL/glx.h>
    #include <X11/Xlib.h>
    #include <X11/Xutil.h>
#endif

/* ============================================================================
 * PLATFORM ABSTRACTION STRUCTURES
 * ============================================================================ */

typedef struct {
    int width;
    int height;
    const char* title;
    bool fullscreen;
    bool vsync;
    int gl_major;
    int gl_minor;
} WindowConfig;

typedef struct {
    void* native_handle;
    void* gl_context;
    int width;
    int height;
    bool is_open;
    double last_time;
    double delta_time;
} WindowState;

/* Global window state */
static WindowState g_window = {0};

/* ============================================================================
 * PLATFORM-SPECIFIC IMPLEMENTATIONS
 * ============================================================================ */

#ifdef _WIN32
/* Windows implementation */

static LRESULT CALLBACK WindowProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    switch (msg) {
        case WM_CLOSE:
            g_window.is_open = false;
            return 0;
        case WM_SIZE:
            g_window.width = LOWORD(lParam);
            g_window.height = HIWORD(lParam);
            glViewport(0, 0, g_window.width, g_window.height);
            return 0;
        case WM_DESTROY:
            PostQuitMessage(0);
            return 0;
    }
    return DefWindowProc(hwnd, msg, wParam, lParam);
}

int native_create_window(WindowConfig* config) {
    WNDCLASSEX wc = {0};
    wc.cbSize = sizeof(WNDCLASSEX);
    wc.style = CS_HREDRAW | CS_VREDRAW | CS_OWNDC;
    wc.lpfnWndProc = WindowProc;
    wc.hInstance = GetModuleHandle(NULL);
    wc.hCursor = LoadCursor(NULL, IDC_ARROW);
    wc.lpszClassName = "PyFlareWindow";

    if (!RegisterClassEx(&wc)) {
        printf("Failed to register window class\n");
        return 0;
    }

    DWORD style = WS_OVERLAPPEDWINDOW;
    if (config->fullscreen) {
        style = WS_POPUP;
    }

    RECT rect = {0, 0, config->width, config->height};
    AdjustWindowRect(&rect, style, FALSE);

    HWND hwnd = CreateWindowEx(
        0, "PyFlareWindow", config->title, style,
        CW_USEDEFAULT, CW_USEDEFAULT,
        rect.right - rect.left, rect.bottom - rect.top,
        NULL, NULL, GetModuleHandle(NULL), NULL
    );

    if (!hwnd) {
        printf("Failed to create window\n");
        return 0;
    }

    /* Set up OpenGL context */
    HDC hdc = GetDC(hwnd);
    
    PIXELFORMATDESCRIPTOR pfd = {0};
    pfd.nSize = sizeof(PIXELFORMATDESCRIPTOR);
    pfd.nVersion = 1;
    pfd.dwFlags = PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER;
    pfd.iPixelType = PFD_TYPE_RGBA;
    pfd.cColorBits = 24;
    pfd.cDepthBits = 24;
    pfd.cStencilBits = 8;
    pfd.iLayerType = PFD_MAIN_PLANE;

    int pixel_format = ChoosePixelFormat(hdc, &pfd);
    SetPixelFormat(hdc, pixel_format, &pfd);

    HGLRC hglrc = wglCreateContext(hdc);
    wglMakeCurrent(hdc, hglrc);

    /* Store window state */
    g_window.native_handle = hwnd;
    g_window.gl_context = hglrc;
    g_window.width = config->width;
    g_window.height = config->height;
    g_window.is_open = true;
    g_window.last_time = (double)GetTickCount() / 1000.0;

    ShowWindow(hwnd, SW_SHOW);
    UpdateWindow(hwnd);

    return 1;
}

void native_swap_buffers() {
    HDC hdc = GetDC((HWND)g_window.native_handle);
    SwapBuffers(hdc);
    ReleaseDC((HWND)g_window.native_handle, hdc);
}

void native_poll_events() {
    MSG msg;
    while (PeekMessage(&msg, NULL, 0, 0, PM_REMOVE)) {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }
}

#else
/* Linux/X11 implementation */

static Display* g_display = NULL;
static Window g_x_window = 0;
static GLXContext g_glx_context = NULL;

int native_create_window(WindowConfig* config) {
    g_display = XOpenDisplay(NULL);
    if (!g_display) {
        printf("Failed to open X display\n");
        return 0;
    }

    int screen = DefaultScreen(g_display);
    
    /* Get visual info for OpenGL */
    int visual_attribs[] = {
        GLX_RGBA,
        GLX_DEPTH_SIZE, 24,
        GLX_DOUBLEBUFFER,
        None
    };
    
    XVisualInfo* vi = glXChooseVisual(g_display, screen, visual_attribs);
    if (!vi) {
        printf("Failed to choose visual\n");
        return 0;
    }

    /* Create window */
    Window root = RootWindow(g_display, screen);
    
    XSetWindowAttributes swa = {0};
    swa.colormap = XCreateColormap(g_display, root, vi->visual, AllocNone);
    swa.event_mask = ExposureMask | KeyPressMask | StructureNotifyMask;

    g_x_window = XCreateWindow(
        g_display, root,
        0, 0, config->width, config->height, 0,
        vi->depth, InputOutput, vi->visual,
        CWColormap | CWEventMask, &swa
    );

    XMapWindow(g_display, g_x_window);
    XStoreName(g_display, g_x_window, config->title);

    /* Create OpenGL context */
    g_glx_context = glXCreateContext(g_display, vi, NULL, GL_TRUE);
    glXMakeCurrent(g_display, g_x_window, g_glx_context);

    XFree(vi);

    /* Store window state */
    g_window.native_handle = (void*)(long)g_x_window;
    g_window.gl_context = g_glx_context;
    g_window.width = config->width;
    g_window.height = config->height;
    g_window.is_open = true;

    return 1;
}

void native_swap_buffers() {
    glXSwapBuffers(g_display, g_x_window);
}

void native_poll_events() {
    XEvent event;
    while (XPending(g_display)) {
        XNextEvent(g_display, &event);
        
        switch (event.type) {
            case ClientMessage:
                g_window.is_open = false;
                break;
            case ConfigureNotify:
                g_window.width = event.xconfigure.width;
                g_window.height = event.xconfigure.height;
                glViewport(0, 0, g_window.width, g_window.height);
                break;
        }
    }
}

#endif

/* ============================================================================
 * PLATFORM-INDEPENDENT API
 * ============================================================================ */

int native_init_window(int width, int height, const char* title, int fullscreen, int vsync) {
    WindowConfig config = {0};
    config.width = width;
    config.height = height;
    config.title = title;
    config.fullscreen = fullscreen != 0;
    config.vsync = vsync != 0;
    config.gl_major = 2;
    config.gl_minor = 1;

    if (!native_create_window(&config)) {
        return 0;
    }

    /* Initialize OpenGL state */
    glViewport(0, 0, width, height);
    glClearColor(0.2f, 0.2f, 0.25f, 1.0f);
    glEnable(GL_BLEND);
    glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
    
    /* Enable depth testing for 3D */
    glEnable(GL_DEPTH_TEST);
    glDepthFunc(GL_LEQUAL);

    printf("PyFlare Native Window Created: %dx%d\n", width, height);
    printf("OpenGL Version: %s\n", glGetString(GL_VERSION));
    printf("OpenGL Vendor: %s\n", glGetString(GL_VENDOR));
    printf("OpenGL Renderer: %s\n", glGetString(GL_RENDERER));

    return 1;
}

void native_destroy_window() {
    #ifdef _WIN32
        if (g_window.gl_context) {
            wglMakeCurrent(NULL, NULL);
            wglDeleteContext((HGLRC)g_window.gl_context);
        }
        if (g_window.native_handle) {
            DestroyWindow((HWND)g_window.native_handle);
        }
    #else
        if (g_glx_context) {
            glXMakeCurrent(g_display, None, NULL);
            glXDestroyContext(g_display, g_glx_context);
        }
        if (g_x_window) {
            XDestroyWindow(g_display, g_x_window);
        }
        if (g_display) {
            XCloseDisplay(g_display);
        }
    #endif

    g_window.is_open = false;
    printf("PyFlare Native Window Destroyed\n");
}

void native_update() {
    native_poll_events();
    
    /* Calculate delta time */
    #ifdef _WIN32
        double current_time = (double)GetTickCount() / 1000.0;
    #else
        struct timespec ts;
        clock_gettime(CLOCK_MONOTONIC, &ts);
        double current_time = ts.tv_sec + ts.tv_nsec / 1000000000.0;
    #endif
    
    g_window.delta_time = current_time - g_window.last_time;
    g_window.last_time = current_time;
}

void native_present() {
    native_swap_buffers();
}

int native_is_window_open() {
    return g_window.is_open ? 1 : 0;
}

void native_get_window_size(int* width, int* height) {
    *width = g_window.width;
    *height = g_window.height;
}

double native_get_delta_time() {
    return g_window.delta_time;
}

/* ============================================================================
 * OPENGL UTILITY FUNCTIONS
 * ============================================================================ */

void native_clear(float r, float g, float b, float a) {
    glClearColor(r, g, b, a);
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
}

unsigned int native_create_shader(const char* vertex_src, const char* fragment_src) {
    GLuint vertex_shader = glCreateShader(GL_VERTEX_SHADER);
    glShaderSource(vertex_shader, 1, &vertex_src, NULL);
    glCompileShader(vertex_shader);

    GLint success;
    glGetShaderiv(vertex_shader, GL_COMPILE_STATUS, &success);
    if (!success) {
        char info_log[512];
        glGetShaderInfoLog(vertex_shader, 512, NULL, info_log);
        printf("Vertex shader compilation failed: %s\n", info_log);
        return 0;
    }

    GLuint fragment_shader = glCreateShader(GL_FRAGMENT_SHADER);
    glShaderSource(fragment_shader, 1, &fragment_src, NULL);
    glCompileShader(fragment_shader);

    glGetShaderiv(fragment_shader, GL_COMPILE_STATUS, &success);
    if (!success) {
        char info_log[512];
        glGetShaderInfoLog(fragment_shader, 512, NULL, info_log);
        printf("Fragment shader compilation failed: %s\n", info_log);
        return 0;
    }

    GLuint program = glCreateProgram();
    glAttachShader(program, vertex_shader);
    glAttachShader(program, fragment_shader);
    glLinkProgram(program);

    glGetProgramiv(program, GL_LINK_STATUS, &success);
    if (!success) {
        char info_log[512];
        glGetProgramInfoLog(program, 512, NULL, info_log);
        printf("Shader program linking failed: %s\n", info_log);
        return 0;
    }

    glDeleteShader(vertex_shader);
    glDeleteShader(fragment_shader);

    return program;
}

void native_use_shader(unsigned int shader_id) {
    glUseProgram(shader_id);
}

void native_delete_shader(unsigned int shader_id) {
    glDeleteProgram(shader_id);
}

/* ============================================================================
 * MEMORY AND PERFORMANCE UTILITIES
 * ============================================================================ */

long native_get_memory_usage() {
    #ifdef _WIN32
        PROCESS_MEMORY_COUNTERS pmc;
        if (GetProcessMemoryInfo(GetCurrentProcess(), &pmc, sizeof(pmc))) {
            return (long)pmc.WorkingSetSize;
        }
    #endif
    return 0;
}

double native_get_time() {
    #ifdef _WIN32
        return (double)GetTickCount() / 1000.0;
    #else
        struct timespec ts;
        clock_gettime(CLOCK_MONOTONIC, &ts);
        return ts.tv_sec + ts.tv_nsec / 1000000000.0;
    #endif
}