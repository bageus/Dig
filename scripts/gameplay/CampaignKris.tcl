// ######################### Campaign load ##########################
map create 512 640
sm_draw_stone -border

generate_color_variation 0 0 512 640 0
#set_fow_begin 33
set_light_begin 33
#set_view_begin 23
set_view 292.7 28.8 1.478 -0.29 0.05		;# set inital camera view (x y zoom)

set temppos {256 16};call templates/tcl/kris_unq_start_skirm.tcl
set temppos {260 12};call templates/tcl/kris_gng_024_a.tcl;MapTemplateSet 260 12

sm_set_temp {{swf_uebergang_kris 256 16}}

sel /obj
set FR [new FogRemover]
set_pos $FR {280 14 15}
call_method $FR fog_remove 0 46 22
adaptive_sound marker kristall {280 14 15} 1000
adaptive_sound changethemenow kristall


show_loading 0

#set_brightness 2

log "CampaignKris.tcl loaded..."


sm_force_zone Kristall
