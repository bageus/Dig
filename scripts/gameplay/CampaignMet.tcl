// ######################### Campaign load ##########################
map create 512 640
sm_draw_stone -border

generate_color_variation 0 0 512 640 0
#set_fow_begin 33
set_light_begin 33
#set_view_begin 23
set_view 292.7 28.8 1.478 -0.29 0.05		;# set inital camera view (x y zoom)

//set temppos {256 4};call templates/tcl/urw_unq_metalltor.tcl
set temppos {260 28};call templates/tcl/swf_unq_start_skirm.tcl
set temppos {312 32};call templates/tcl/swf_unq_start2.tcl
set temppos {368 44};call templates/tcl/swf_gng_022_c.tcl;MapTemplateSet 368 44
set temppos {304 24};call templates/tcl/swf_gng_024_a.tcl;MapTemplateSet 304 24

sm_set_temp {{urw_uebergang_swf 256 4}}

sel /obj
set FR [new FogRemover]
set_pos $FR {280 14 15}
call_method $FR fog_remove 0 46 22
adaptive_sound marker metall {280 14 15} 1000
adaptive_sound changethemenow metall

show_loading 0

#set_brightness 2

log "CampaignMet.tcl loaded..."


sm_force_zone Metall
