/* PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
 *
 * The macOS smoke test of vtkNative. It draws a hexahedron and a tetrahedron with a caption
 * 1. in an off screen view (vtkn_view_create with off_screen 1), written with vtkn_view_write_png, plain and with
 *    the ambient occlusion on;
 * 2. in an external view drawn with a CGL context and a framebuffer of the test, as the Avalonia host draws it:
 *    attach, render_external into the framebuffer, read back and written as PNG; then rotated by injected middle
 *    button input, presented again, presented stretched into a larger framebuffer, and detached.
 * Every picture is checked for not being a single color. The PNG files are written into the folder given as the
 * first argument (default: the current folder).
 */
#include "vtkNative.h"

#include <OpenGL/OpenGL.h>
#include <OpenGL/gl3.h>
#include <zlib.h>

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static int failures = 0;

static void check(vtkn_status status, const char* call)
{
    if (status == VTKN_OK) return;
    char message[2048];
    int32_t required = 0;
    vtkn_get_last_error(message, (int32_t)sizeof(message), &required);
    fprintf(stderr, "FAIL %s: status %d: %s\n", call, (int)status, message);
    failures++;
}
#define CHECK(call) check((call), #call)

static void expect(int condition, const char* what)
{
    printf("%s %s\n", condition ? "ok  " : "FAIL", what);
    if (!condition) failures++;
}

/* PNG ------------------------------------------------------------------------------------------------------------ */
static void write_chunk(FILE* file, const char* type, const unsigned char* data, uint32_t length)
{
    unsigned char header[8] = { (unsigned char)(length >> 24), (unsigned char)(length >> 16),
                                (unsigned char)(length >> 8), (unsigned char)length,
                                (unsigned char)type[0], (unsigned char)type[1],
                                (unsigned char)type[2], (unsigned char)type[3] };
    fwrite(header, 1, 8, file);
    if (length > 0) fwrite(data, 1, length, file);
    uLong crc = crc32(0, header + 4, 4);
    if (length > 0) crc = crc32(crc, data, length);
    unsigned char tail[4] = { (unsigned char)(crc >> 24), (unsigned char)(crc >> 16), (unsigned char)(crc >> 8),
                              (unsigned char)crc };
    fwrite(tail, 1, 4, file);
}

/* RGBA pixels, bottom row first as OpenGL reads them */
static void write_rgba_png(const char* path, const unsigned char* rgba, int width, int height)
{
    size_t rowSize = (size_t)width * 4 + 1;
    unsigned char* raw = malloc(rowSize * height);
    for (int y = 0; y < height; y++)
    {
        raw[y * rowSize] = 0;
        memcpy(raw + y * rowSize + 1, rgba + (size_t)(height - 1 - y) * width * 4, (size_t)width * 4);
    }
    uLongf packedSize = compressBound((uLong)(rowSize * height));
    unsigned char* packed = malloc(packedSize);
    compress2(packed, &packedSize, raw, (uLong)(rowSize * height), 6);
    FILE* file = fopen(path, "wb");
    if (file == NULL)
    {
        fprintf(stderr, "FAIL cannot write %s\n", path);
        failures++;
    }
    else
    {
        static const unsigned char signature[8] = { 137, 80, 78, 71, 13, 10, 26, 10 };
        unsigned char ihdr[13] = { (unsigned char)(width >> 24), (unsigned char)(width >> 16),
                                   (unsigned char)(width >> 8), (unsigned char)width,
                                   (unsigned char)(height >> 24), (unsigned char)(height >> 16),
                                   (unsigned char)(height >> 8), (unsigned char)height, 8, 6, 0, 0, 0 };
        fwrite(signature, 1, 8, file);
        write_chunk(file, "IHDR", ihdr, 13);
        write_chunk(file, "IDAT", packed, (uint32_t)packedSize);
        write_chunk(file, "IEND", NULL, 0);
        fclose(file);
        printf("     wrote %s\n", path);
    }
    free(packed);
    free(raw);
}

/* The number of pixels that differ from the bottom left one, a background pixel */
static int count_non_background(const unsigned char* pixels, int width, int height, int channels)
{
    int count = 0;
    for (int i = 0; i < width * height; i++)
    {
        if (memcmp(pixels + (size_t)i * channels, pixels, 3) != 0) count++;
    }
    return count;
}

/* Scene ---------------------------------------------------------------------------------------------------------- */
static void add_scene(vtkn_view view)
{
    const double top[3] = { 0.85, 0.88, 0.95 };
    const double bottom[3] = { 0.35, 0.40, 0.50 };
    CHECK(vtkn_view_set_background(view, 1, top, bottom));
    /* A unit hexahedron and a tetrahedron beside it */
    const double points[12 * 3] = { 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0,
                                    0, 0, 1, 1, 0, 1, 1, 1, 1, 0, 1, 1,
                                    1.5, 0, 0, 2.5, 0, 0, 1.5, 1, 0, 1.5, 0, 1 };
    const int32_t cells[9 + 5] = { 8, 0, 1, 2, 3, 4, 5, 6, 7, 4, 8, 9, 10, 11 };
    const uint8_t types[2] = { 12, 10 };
    const double color[3] = { 0.2, 0.6, 1.0 };
    CHECK(vtkn_view_add_mesh_actor(view, "mesh", points, 12, cells, 14, types, 2, color, 1));
    const double position[3] = { 0.5, 1, 1 };
    CHECK(vtkn_view_add_caption(view, "caption", "vtkNative on macOS", position, NULL, 16, VTKN_LAYER_BASE, 0));
    CHECK(vtkn_view_set_standard_view(view, VTKN_VIEW_ISOMETRIC, 1));
}

static void test_off_screen(const char* folder)
{
    printf("off screen view\n");
    vtkn_view view = NULL;
    CHECK(vtkn_view_create(1, &view));
    if (view == NULL) return;
    CHECK(vtkn_view_set_size(view, 640, 480));
    add_scene(view);
    CHECK(vtkn_view_render(view));
    char info[4096];
    int32_t required = 0;
    CHECK(vtkn_view_get_render_info(view, info, (int32_t)sizeof(info), &required));
    printf("     %.300s\n", info);
    char path[1024];
    snprintf(path, sizeof(path), "%s/offscreen.png", folder);
    CHECK(vtkn_view_write_png(view, path, 1));
    printf("     wrote %s\n", path);
    /* The pixels of the same picture are checked */
    int32_t width = 0;
    int32_t height = 0;
    unsigned char* pixels = malloc(640 * 480 * 3);
    CHECK(vtkn_view_read_pixels(view, 1, pixels, 640 * 480 * 3, &width, &height));
    expect(width == 640 && height == 480, "off screen picture has the view size");
    int drawn = count_non_background(pixels, width, height, 3);
    printf("     %d of %d pixels differ from the corner\n", drawn, width * height);
    expect(drawn > width * height / 2, "off screen picture is not blank (gradient and geometry)");
    const unsigned char* bottomLeft = pixels;
    const unsigned char* topLeft = pixels + (size_t)(height - 1) * width * 3;
    printf("     background bottom %d %d %d, top %d %d %d\n", bottomLeft[0], bottomLeft[1], bottomLeft[2], topLeft[0],
           topLeft[1], topLeft[2]);
    /* The ambient occlusion: the gradient background stays only with the patch of vtkSSAOPass */
    CHECK(vtkn_view_set_ambient_occlusion(view, 1, 0.05, 32));
    CHECK(vtkn_view_render(view));
    snprintf(path, sizeof(path), "%s/offscreen_ao.png", folder);
    CHECK(vtkn_view_write_png(view, path, 1));
    printf("     wrote %s\n", path);
    CHECK(vtkn_view_read_pixels(view, 1, pixels, 640 * 480 * 3, &width, &height));
    bottomLeft = pixels;
    topLeft = pixels + (size_t)(height - 1) * width * 3;
    printf("     with ambient occlusion: background bottom %d %d %d, top %d %d %d\n", bottomLeft[0], bottomLeft[1],
           bottomLeft[2], topLeft[0], topLeft[1], topLeft[2]);
    /* Not counted as a failure: only a VTK built with patches/vtk-9.7.1-ssao-background.patch keeps it */
    printf("%s gradient background kept with ambient occlusion\n",
           memcmp(bottomLeft, topLeft, 3) != 0 ? "ok  " : "KNOWN (VTK without the SSAO patch):");
    free(pixels);
    CHECK(vtkn_view_destroy(view));
}

static int renderEndCount = 0;

static void on_event(void* userData, int32_t eventId, const double* values, int32_t valueCount)
{
    (void)userData;
    (void)values;
    (void)valueCount;
    if (eventId == VTKN_EVENT_RENDER_END) renderEndCount++;
}

/* External view ------------------------------------------------------------------------------------------------ */
typedef struct target
{
    GLuint framebuffer;
    GLuint color;
    int width;
    int height;
} target;

static target create_target(int width, int height)
{
    target t = { 0, 0, width, height };
    glGenFramebuffers(1, &t.framebuffer);
    glBindFramebuffer(GL_FRAMEBUFFER, t.framebuffer);
    glGenRenderbuffers(1, &t.color);
    glBindRenderbuffer(GL_RENDERBUFFER, t.color);
    glRenderbufferStorage(GL_RENDERBUFFER, GL_RGBA8, width, height);
    glFramebufferRenderbuffer(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_RENDERBUFFER, t.color);
    expect(glCheckFramebufferStatus(GL_FRAMEBUFFER) == GL_FRAMEBUFFER_COMPLETE, "host framebuffer complete");
    return t;
}

static void destroy_target(target* t)
{
    glDeleteFramebuffers(1, &t->framebuffer);
    glDeleteRenderbuffers(1, &t->color);
}

/* What the draw callback of the host does: bind its framebuffer, clear it, let the view present into it */
static unsigned char* present(vtkn_view view, const target* t, const char* folder, const char* name)
{
    glBindFramebuffer(GL_FRAMEBUFFER, t->framebuffer);
    glViewport(0, 0, t->width, t->height);
    glClearColor(1, 0, 1, 1);
    glClear(GL_COLOR_BUFFER_BIT);
    CHECK(vtkn_view_render_external(view, t->framebuffer, t->width, t->height));
    /* The host still has its framebuffer bound after the call */
    GLint bound = 0;
    glGetIntegerv(GL_DRAW_FRAMEBUFFER_BINDING, &bound);
    expect(bound == (GLint)t->framebuffer, "host framebuffer still bound after render_external");
    unsigned char* rgba = malloc((size_t)t->width * t->height * 4);
    glBindFramebuffer(GL_READ_FRAMEBUFFER, t->framebuffer);
    glPixelStorei(GL_PACK_ALIGNMENT, 1);
    glReadPixels(0, 0, t->width, t->height, GL_RGBA, GL_UNSIGNED_BYTE, rgba);
    expect(glGetError() == GL_NO_ERROR, "no OpenGL error after reading the host framebuffer");
    char path[1024];
    snprintf(path, sizeof(path), "%s/%s.png", folder, name);
    write_rgba_png(path, rgba, t->width, t->height);
    int magenta = 0;
    for (int i = 0; i < t->width * t->height; i++)
    {
        if (rgba[i * 4] == 255 && rgba[i * 4 + 1] == 0 && rgba[i * 4 + 2] == 255) magenta++;
    }
    expect(magenta == 0, "the frame covers the whole host framebuffer");
    int drawn = count_non_background(rgba, t->width, t->height, 4);
    printf("     %d of %d pixels differ from the corner\n", drawn, t->width * t->height);
    expect(drawn > t->width * t->height / 2, "external picture is not blank");
    return rgba;
}

static void test_external(const char* folder)
{
    printf("external view\n");
    CGLPixelFormatAttribute attributes[] = { kCGLPFAOpenGLProfile, (CGLPixelFormatAttribute)kCGLOGLPVersion_GL4_Core,
                                             kCGLPFAAccelerated, kCGLPFAColorSize, (CGLPixelFormatAttribute)24,
                                             kCGLPFAAlphaSize, (CGLPixelFormatAttribute)8, 0 };
    CGLPixelFormatObj pixelFormat = NULL;
    GLint count = 0;
    CGLContextObj context = NULL;
    if (CGLChoosePixelFormat(attributes, &pixelFormat, &count) != kCGLNoError ||
        CGLCreateContext(pixelFormat, NULL, &context) != kCGLNoError)
    {
        expect(0, "host CGL context");
        return;
    }
    CGLReleasePixelFormat(pixelFormat);
    CGLSetCurrentContext(context);
    printf("     host context: %s, %s\n", glGetString(GL_RENDERER), glGetString(GL_VERSION));

    vtkn_view view = NULL;
    CHECK(vtkn_view_create_external(&view));
    if (view == NULL) return;
    /* Calls before the context exists draw nothing and fail nothing */
    CHECK(vtkn_view_set_size(view, 400, 300));
    add_scene(view);
    CHECK(vtkn_view_render(view));
    CHECK(vtkn_view_attach_external_context(view));
    CHECK(vtkn_view_set_event_callback(view, on_event, NULL));

    target t = create_target(400, 300);
    unsigned char* first = present(view, &t, folder, "external");

    /* A drag of the middle button rotates the camera (the left one selects, as in PrePoMax); the style renders,
       the host presents */
    CHECK(vtkn_view_handle_input(view, VTKN_INPUT_MIDDLE_BUTTON_DOWN, 200, 150, 0, 0, 0, NULL));
    for (int i = 1; i <= 10; i++)
        CHECK(vtkn_view_handle_input(view, VTKN_INPUT_MOUSE_MOVE, 200 + 8 * i, 150 + 3 * i, 0, 0, 0, NULL));
    CHECK(vtkn_view_handle_input(view, VTKN_INPUT_MIDDLE_BUTTON_UP, 280, 180, 0, 0, 0, NULL));
    printf("     %d renders raised VTKN_EVENT_RENDER_END during the drag\n", renderEndCount);
    expect(renderEndCount > 0, "the drag rendered the view (the host is told to present)");
    /* The host draws with its context in between: another framebuffer bound, other state */
    glBindFramebuffer(GL_FRAMEBUFFER, 0);
    glDisable(GL_DEPTH_TEST);
    unsigned char* rotated = present(view, &t, folder, "external_rotated");
    expect(memcmp(first, rotated, (size_t)t.width * t.height * 4) != 0, "injected drag changed the picture");

    /* A pick renders and reads back outside the draw callback of the host; the frame it shows stays the same */
    vtkn_point_pick pick;
    char partName[256];
    int32_t partRequired = 0;
    CHECK(vtkn_view_pick_point(view, 120, 140, 5, 0, NULL, &pick, partName, (int32_t)sizeof(partName),
                               &partRequired));
    /* The mesh actor is not a part, so nothing is hit; the pick still renders the selection passes */
    printf("     pick at 120, 140: hit %d\n", (int)pick.hit);
    glBindFramebuffer(GL_FRAMEBUFFER, 0);
    unsigned char* afterPick = present(view, &t, folder, "external_after_pick");
    expect(memcmp(afterPick, rotated, (size_t)t.width * t.height * 4) == 0, "the frame after a pick is unchanged");
    free(afterPick);
    free(first);
    free(rotated);

    /* The frame stretched into a framebuffer twice the size (a Retina host that keeps the view at points) */
    target large = create_target(800, 600);
    free(present(view, &large, folder, "external_stretched"));
    destroy_target(&large);

    /* The view at the size of the framebuffer, as a Retina host that draws in pixels */
    CHECK(vtkn_view_set_size(view, 800, 600));
    large = create_target(800, 600);
    free(present(view, &large, folder, "external_resized"));
    destroy_target(&large);

    /* Without its context current the view refuses to present */
    CGLSetCurrentContext(NULL);
    expect(vtkn_view_render_external(view, t.framebuffer, t.width, t.height) != VTKN_OK,
           "render_external fails without the context current");
    CGLSetCurrentContext(context);

    /* The host destroys its context: detach first, then the view can live on and be attached again */
    CHECK(vtkn_view_detach_external_context(view));
    CHECK(vtkn_view_render(view));
    destroy_target(&t);
    CGLSetCurrentContext(NULL);
    CGLDestroyContext(context);

    if (CGLChoosePixelFormat(attributes, &pixelFormat, &count) == kCGLNoError &&
        CGLCreateContext(pixelFormat, NULL, &context) == kCGLNoError)
    {
        CGLReleasePixelFormat(pixelFormat);
        CGLSetCurrentContext(context);
        CHECK(vtkn_view_attach_external_context(view));
        target again = create_target(800, 600);
        free(present(view, &again, folder, "external_reattached"));
        destroy_target(&again);
        CHECK(vtkn_view_detach_external_context(view));
        CGLSetCurrentContext(NULL);
        CGLDestroyContext(context);
    }
    CHECK(vtkn_view_destroy(view));
}

int main(int argc, char** argv)
{
    const char* folder = argc > 1 ? argv[1] : ".";
    char version[64];
    int32_t required = 0;
    CHECK(vtkn_get_vtk_version(version, (int32_t)sizeof(version), &required));
    printf("vtkNative API %d, VTK %s\n", vtkn_get_api_version(), version);
    CHECK(vtkn_set_font_files("/System/Library/Fonts/Supplemental/Arial.ttf",
                              "/System/Library/Fonts/Supplemental/Arial Bold.ttf", NULL, NULL));
    expect(vtkn_set_font_files("/nonexistent/font.ttf", NULL, NULL, NULL) == VTKN_ERROR_INVALID_ARGUMENT,
           "a missing font file is refused");
    test_off_screen(folder);
    test_external(folder);
    printf(failures == 0 ? "PASSED\n" : "FAILED: %d\n", failures);
    return failures == 0 ? 0 : 1;
}
