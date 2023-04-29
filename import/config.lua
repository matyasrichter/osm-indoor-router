-- Based on the generic.lua config from https://github.com/openstreetmap/osm2pgsql/tree/master/flex-config

-- Set this to the projection you want to use
local srid = 4326

local now = os.date()

local tables = {}

tables.points = osm2pgsql.define_node_table('osm_points', {
    { column = 'tags', type = 'jsonb' },
    { column = 'geom', type = 'point', projection = srid, not_null = true },
    { column = 'updated_at', sql_type = 'timestamp' },
})

tables.lines = osm2pgsql.define_way_table('osm_lines', {
    { column = 'tags', type = 'jsonb' },
    { column = 'geom', type = 'linestring', projection = srid, not_null = true },
    { column = 'nodes', sql_type = 'bigint[]', not_null = true },
    { column = 'updated_at', sql_type = 'timestamp' },
})

tables.polygons = osm2pgsql.define_area_table('osm_polygons', {
    { column = 'tags', type = 'jsonb' },
    { column = 'geom', type = 'polygon', projection = srid, not_null = true },
    { column = 'geom_linestring', type = 'linestring', projection = srid, not_null = true },
    { column = 'nodes', sql_type = 'bigint[]', not_null = true },
    { column = 'updated_at', sql_type = 'timestamp' },
})

tables.multipolygons = osm2pgsql.define_table({
    name = 'osm_multipolygons',
    columns = {
        { column = 'area_id', type = 'bigint' },
        { column = 'tags', type = 'jsonb' },
        { column = 'geom', type = 'multipolygon', projection = srid, not_null = true },
        { column = 'updated_at', sql_type = 'timestamp' },
    }
})

tables.multipolygons_m2m = osm2pgsql.define_table({
    name = 'osm_multipolygons_m2m',
    columns = {
        { column = 'mp_id', sql_type = 'bigint' },
        { column = 'l_id', sql_type = 'bigint' },
    }
})


-- These tag keys are generally regarded as useless for most rendering. Most
-- of them are from imports or intended as internal information for mappers.
--
-- If a key ends in '*' it will match all keys with the specified prefix.
--
-- If you want some of these keys, perhaps for a debugging layer, just
-- delete the corresponding lines.
local delete_keys = {
    -- "mapper" keys
    'attribution',
    'comment',
    'created_by',
    'fixme',
    'note',
    'note:*',
    'odbl',
    'odbl:note',
    'source',
    'source:*',
    'source_ref',

    -- "import" keys

    -- Corine Land Cover (CLC) (Europe)
    'CLC:*',

    -- Geobase (CA)
    'geobase:*',
    -- CanVec (CA)
    'canvec:*',

    -- osak (DK)
    'osak:*',
    -- kms (DK)
    'kms:*',

    -- ngbe (ES)
    -- See also note:es and source:file above
    'ngbe:*',

    -- Friuli Venezia Giulia (IT)
    'it:fvg:*',

    -- KSJ2 (JA)
    -- See also note:ja and source_ref above
    'KSJ2:*',
    -- Yahoo/ALPS (JA)
    'yh:*',

    -- LINZ (NZ)
    'LINZ2OSM:*',
    'linz2osm:*',
    'LINZ:*',
    'ref:linz:*',

    -- WroclawGIS (PL)
    'WroclawGIS:*',
    -- Naptan (UK)
    'naptan:*',

    -- TIGER (US)
    'tiger:*',
    -- GNIS (US)
    'gnis:*',
    -- National Hydrography Dataset (US)
    'NHD:*',
    'nhd:*',
    -- mvdgis (Montevideo, UY)
    'mvdgis:*',

    -- EUROSHA (Various countries)
    'project:eurosha_2012',

    -- UrbIS (Brussels, BE)
    'ref:UrbIS',

    -- NHN (CA)
    'accuracy:meters',
    'sub_sea:type',
    'waterway:type',
    -- StatsCan (CA)
    'statscan:rbuid',

    -- RUIAN (CZ)
    'ref:ruian:addr',
    'ref:ruian',
    'building:ruian:type',
    -- DIBAVOD (CZ)
    'dibavod:id',
    -- UIR-ADR (CZ)
    'uir_adr:ADRESA_KOD',

    -- GST (DK)
    'gst:feat_id',

    -- Maa-amet (EE)
    'maaamet:ETAK',
    -- FANTOIR (FR)
    'ref:FR:FANTOIR',

    -- 3dshapes (NL)
    '3dshapes:ggmodelk',
    -- AND (NL)
    'AND_nosr_r',

    -- OPPDATERIN (NO)
    'OPPDATERIN',
    -- Various imports (PL)
    'addr:city:simc',
    'addr:street:sym_ul',
    'building:usage:pl',
    'building:use:pl',
    -- TERYT (PL)
    'teryt:simc',

    -- RABA (SK)
    'raba:id',
    -- DCGIS (Washington DC, US)
    'dcgis:gis_id',
    -- Building Identification Number (New York, US)
    'nycdoitt:bin',
    -- Chicago Building Inport (US)
    'chicago:building_id',
    -- Louisville, Kentucky/Building Outlines Import (US)
    'lojic:bgnum',
    -- MassGIS (Massachusetts, US)
    'massgis:way_id',
    -- Los Angeles County building ID (US)
    'lacounty:*',
    -- Address import from Bundesamt für Eich- und Vermessungswesen (AT)
    'at_bev:addr_date',

    -- misc
    'import',
    'import_uuid',
    'OBJTYPE',
    'SK53_bulk:load',
    'mml:class'
}

-- The osm2pgsql.make_clean_tags_func() function takes the list of keys
-- and key prefixes defined above and returns a function that can be used
-- to clean those tags out of a Lua table. The clean_tags function will
-- return true if it removed all tags from the table.
local clean_tags = osm2pgsql.make_clean_tags_func(delete_keys)

-- Helper function that looks at the tags and decides if this is possibly
-- an area.
function has_area_tags(tags)
    if tags.area == 'yes' then
        return true
    end
    if tags.area == 'no' then
        return false
    end

    return tags.aeroway
        or tags.amenity
        or tags.building
        or tags.harbour
        or tags.historic
        or tags.landuse
        or tags.leisure
        or tags.man_made
        or tags.military
        or tags.natural
        or tags.office
        or tags.place
        or tags.power
        or tags.public_transport
        or tags.shop
        or tags.sport
        or tags.tourism
        or tags.water
        or tags.waterway
        or tags.wetland
        or tags['abandoned:aeroway']
        or tags['abandoned:amenity']
        or tags['abandoned:building']
        or tags['abandoned:landuse']
        or tags['abandoned:power']
        or tags['area:highway']
        or tags['building:part']
        or tags.indoor == 'room'
        or tags.indoor == 'corridor'
        or tags.indoor == 'hall'
        or tags.indoor == 'area'
        or tags.indoor == 'level'
end

function osm2pgsql.process_node(object)
    if clean_tags(object.tags) then
        return
    end

    tables.points:insert({
        tags = object.tags,
        geom = object:as_point(),
        updated_at = now
    })
end

-- This will be used to store information about relations queryable by member
-- way id. It is a table of tables. The outer table is indexed by the way id,
-- the inner table indexed by the relation id. This way even if the information
-- about a relation is added twice, it will be in there only once. It is
-- always good to write your osm2pgsql Lua code in an idempotent way, i.e.
-- it can be called any number of times and will lead to the same result.
local w2r = {}

function osm2pgsql.process_way(object)
    if clean_tags(object.tags) and not w2r[object.id] then
        return
    end

    if object.is_closed and has_area_tags(object.tags) then
        tables.polygons:insert({
            tags = object.tags,
            geom = object:as_polygon(),
            geom_linestring = object:as_linestring(),
            nodes = '{' .. table.concat(object.nodes,",") .. '}',
            updated_at = now
        })
    else
        tables.lines:insert({
            tags = object.tags,
            geom = object:as_linestring(),
            nodes = '{' .. table.concat(object.nodes,",") .. '}',
            updated_at = now
        })
    end
end

-- This function is called for every added, modified, or deleted relation.
-- Its only job is to return the ids of all member ways of the specified
-- relation we want to see in stage 2 again. It MUST NOT store any information
-- about the relation!
function osm2pgsql.select_relation_members(relation)
    -- Only interested in relations with type=route, route=road and a ref
    if relation.tags.type == 'multipolygon' then
        return { ways = osm2pgsql.way_member_ids(relation) }
    end
end

function osm2pgsql.process_relation(object)
    local relation_type = object:grab_tag('type')

    if clean_tags(object.tags) then
        return
    end

    if relation_type == 'multipolygon' then
        local members = osm2pgsql.way_member_ids(object)
        tables.multipolygons:insert({
            area_id = object.id,
            tags = object.tags,
            geom = object:as_multipolygon(),
            members = '{' .. table.concat(members,",") .. '}',
            updated_at = now
        })
        -- Go through all the members and store relation ids and refs so they
        -- can be found by the way id.
        for _, member in ipairs(object.members) do
            if member.type == 'w' then
                w2r[member.ref] = true
                tables.multipolygons_m2m:insert({
                    mp_id = object.id,
                    l_id = member.ref
                })
            end
        end
    end
end

