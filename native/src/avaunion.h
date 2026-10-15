#pragma once

#include <stdint.h>

#include "avu_resolved.g.h"

#ifdef __cplusplus
extern "C" {
#endif

#if defined(_WIN32)
#define AVU_API __declspec(dllexport)
#else
#define AVU_API __attribute__((visibility("default")))
#endif

#define AVU_ABI_VERSION 2

#define AVU_OK 0
#define AVU_E_ARG (-1)
#define AVU_E_EXCEPTION (-2)
#define AVU_E_FOREIGN_QAPP (-3)
#define AVU_E_NOT_INIT (-4)
#define AVU_E_VERSION (-5)
#define AVU_E_NO_UNION (-6)
#define AVU_E_NO_STYLE (-7)
#define AVU_E_BUFFER (-8)
#define AVU_E_IO (-9)
#define AVU_E_INVALID_PACKAGE (-10)

#define AVU_CHANGE_COLORS 0x01u
#define AVU_CHANGE_STYLE 0x02u
#define AVU_CHANGE_FONTS 0x04u
#define AVU_CHANGE_ICONS 0x08u
#define AVU_CHANGE_ANIMATION 0x10u
#define AVU_CHANGE_LOG 0x20u

#define AVU_PUMP_TIMER 0
#define AVU_PUMP_SHARED 1

#define AVU_FONT_GENERAL 0
#define AVU_FONT_FIXED 1
#define AVU_FONT_SMALL 2
#define AVU_FONT_MENU 3
#define AVU_FONT_TOOLBAR 4
#define AVU_FONT_TITLE 5

#define AVU_MASK_NONE 0
#define AVU_MASK_NORMAL 1
#define AVU_MASK_INVERTED 2

typedef struct avu_style_s *avu_style;
typedef struct avu_element_s *avu_element;

typedef struct avu_probe_result {
    int32_t abi_version;
    int32_t resolved_size;
    char shim_union_version[32];
    char loaded_union_version[32];
} avu_probe_result;

typedef struct avu_init_options {
    int32_t host_runs_glib;
    int32_t force_platform;
} avu_init_options;

typedef struct avu_init_result {
    int32_t pump_mode;
    int32_t platform_theme_loaded;
    int32_t created_application;
    int32_t platform_plugin_id;
    int32_t default_style_id;
    int32_t qt_version_id;
    int32_t union_version_id;
} avu_init_result;

typedef struct avu_style_info {
    int32_t id_string;
    int32_t path_string;
    int32_t has_errors;
    int32_t rule_count;
    int32_t metadata_string;
} avu_style_info;

typedef struct avu_package_info {
    int32_t id_string;
    int32_t path_string;
    int32_t metadata_string;
} avu_package_info;

typedef struct avu_platform_info {
    int32_t smooth_scroll;
    float animation_speed;
    int32_t right_to_left;
    int32_t icon_theme_id;
    int32_t default_style_id;
} avu_platform_info;

typedef struct avu_font_info {
    int32_t family_id;
    float point_size;
    float pixel_size;
    int32_t weight;
    int32_t italic;
} avu_font_info;

typedef void (*avu_callback)(uint32_t change_flags, int32_t argument, void *user);

AVU_API int avu_probe(avu_probe_result *out);
AVU_API int avu_init(const avu_init_options *opts, avu_init_result *out);
AVU_API void avu_shutdown(void);
AVU_API int avu_pump(int max_ms);
AVU_API void avu_set_callback(avu_callback cb, void *user);

AVU_API avu_style avu_style_default(void);
AVU_API avu_style avu_style_by_id(const uint16_t *id, int len);
AVU_API int avu_style_info_get(avu_style s, avu_style_info *out);
AVU_API int avu_style_from_package(const uint16_t *path, int len, avu_style *out);
AVU_API int avu_installed_styles(avu_package_info *out, int cap);

AVU_API avu_element avu_element_create(void);
AVU_API void avu_element_destroy(avu_element e);
AVU_API void avu_element_set_type(avu_element e, const uint16_t *s, int len);
AVU_API void avu_element_set_id(avu_element e, const uint16_t *s, int len);
AVU_API void avu_element_set_states(avu_element e, uint32_t states);
AVU_API void avu_element_set_hints(avu_element e, const uint16_t *s, int len);
AVU_API void avu_element_set_attribute_string(avu_element e, const uint16_t *name, int nlen, const uint16_t *value, int vlen);
AVU_API void avu_element_set_attribute_number(avu_element e, const uint16_t *name, int nlen, double value);
AVU_API void avu_element_clear_attributes(avu_element e);

AVU_API int avu_query(avu_style s, const avu_element *chain, int count, avu_resolved *out);

AVU_API int avu_scheme_color(int group, int set, int role_kind, int role, uint32_t *argb);
AVU_API int avu_set_color_scheme_path(const uint16_t *path, int len);

AVU_API int avu_string(int id, uint16_t *buf, int cap);

AVU_API int avu_icon_render(const uint16_t *name, int len, uint32_t argb_or_zero, int w, int h, float scale, uint8_t *buf, int cap, int *out_w, int *out_h);
AVU_API int avu_image_render(int path_string_id, uint32_t mask_argb, int mask_mode, int w, int h, float scale, uint8_t *buf, int cap, int *out_w, int *out_h);

AVU_API int avu_platform(avu_platform_info *out);
AVU_API int avu_font(int which, avu_font_info *out);

AVU_API int avu_export_style(avu_style s, const uint16_t *out_path, int len);
AVU_API int avu_export_scheme(const uint16_t *colors_file, int len, const uint16_t *out_path, int olen);
AVU_API int avu_export_query(avu_style s, const uint16_t *chains_path, int len, const uint16_t *out_path, int olen);
AVU_API int avu_export_parity(const uint16_t *styles, int slen, const uint16_t *colors, int clen, const uint16_t *chains_path, int len, const uint16_t *out_path, int olen);
AVU_API int avu_export_parity_handles(const avu_style *styles, int count, const uint16_t *colors, int clen, const uint16_t *chains_path, int len, const uint16_t *out_path, int olen);

#ifdef __cplusplus
}
#endif
