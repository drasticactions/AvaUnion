#pragma once

#include "Color.h"
#include "Common.h"
#include "properties/StylePropertyGroup.h"

namespace avu
{
void copyResolved(const Union::Properties::StylePropertyGroup *p, avu_resolved *out);
}
