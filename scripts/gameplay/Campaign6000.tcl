# unique templates and story events definition:
call data/scripts/init/lgtools.tcl

//sm_reset
sm_create_map 512 640

sm_set_digcount 16

// ######################### Campaign init ##########################
map create 512 640 6000 6000
sm_draw_stone -border

generate_color_variation 6000 6000 6512 6640 0
#set_fow_begin 6033
set_light_begin 6033
set_view_begin 6023
set_view 6292.7 6028.8 1.478 -0.29 0.05		;# set inital camera view (x y zoom)

set temppos {6256 6000};call templates/tcl/urw_unq_start.tcl

adaptive_sound marker start {6294 6030 10}
adaptive_sound marker cave {6294 6045 10}

sm_set_temp {{urw_unq_start 6256 0}}

sel /obj
set FR [new FogRemover]
set_pos $FR {6280 6014 15}
call_method $FR fog_remove 0 46 22


show_loading 0

#set_brightness 2

log "Campaign.tcl loaded..."
