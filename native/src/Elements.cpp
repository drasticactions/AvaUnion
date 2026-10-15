#include <QStringList>
#include <QVariantMap>

#include "Common.h"

extern "C" AVU_API avu_element avu_element_create(void)
{
    AVU_GUARD_BEGIN
    return new avu_element_s{Union::Element::create()};
    AVU_GUARD_END(nullptr)
}

extern "C" AVU_API void avu_element_destroy(avu_element e)
{
    AVU_GUARD_BEGIN
    delete e;
    AVU_GUARD_END_VOID
}

extern "C" AVU_API void avu_element_set_type(avu_element e, const uint16_t *s, int len)
{
    AVU_GUARD_BEGIN
    if (e) {
        e->element->setType(avu::fromUtf16(s, len));
    }
    AVU_GUARD_END_VOID
}

extern "C" AVU_API void avu_element_set_id(avu_element e, const uint16_t *s, int len)
{
    AVU_GUARD_BEGIN
    if (e) {
        e->element->setId(avu::fromUtf16(s, len));
    }
    AVU_GUARD_END_VOID
}

extern "C" AVU_API void avu_element_set_states(avu_element e, uint32_t states)
{
    AVU_GUARD_BEGIN
    if (e) {
        e->element->setStates(Union::Element::States::fromInt(int(states)));
    }
    AVU_GUARD_END_VOID
}

extern "C" AVU_API void avu_element_set_hints(avu_element e, const uint16_t *s, int len)
{
    AVU_GUARD_BEGIN
    if (e) {
        const auto text = avu::fromUtf16(s, len);
        e->element->setHints(text.isEmpty() ? QStringList{} : text.split(u'\n', Qt::SkipEmptyParts));
    }
    AVU_GUARD_END_VOID
}

extern "C" AVU_API void avu_element_set_attribute_string(avu_element e, const uint16_t *name, int nlen, const uint16_t *value, int vlen)
{
    AVU_GUARD_BEGIN
    if (e) {
        e->element->setAttribute(avu::fromUtf16(name, nlen), QVariant(avu::fromUtf16(value, vlen)));
    }
    AVU_GUARD_END_VOID
}

extern "C" AVU_API void avu_element_set_attribute_number(avu_element e, const uint16_t *name, int nlen, double value)
{
    AVU_GUARD_BEGIN
    if (e) {
        e->element->setAttribute(avu::fromUtf16(name, nlen), QVariant(value));
    }
    AVU_GUARD_END_VOID
}

extern "C" AVU_API void avu_element_clear_attributes(avu_element e)
{
    AVU_GUARD_BEGIN
    if (e) {
        e->element->setAttributes(QVariantMap{});
    }
    AVU_GUARD_END_VOID
}
