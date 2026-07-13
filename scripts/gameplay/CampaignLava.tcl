// ######################### Campaign load ##########################
map create 512 640
sm_draw_stone -border

generate_color_variation 0 0 512 640 0
#set_fow_begin 33
set_light_begin 33
#set_view_begin 23
set_view 292.7 28.8 1.478 -0.29 0.05		;# set inital camera view (x y zoom)


set temppos {256 20};call templates/tcl/lava_unq_start_skirm.tcl
set temppos {280 16};call templates/tcl/lava_gng_024_a.tcl;MapTemplateSet 280 16

sm_set_temp {{lava_unq_start 256 16}}

sel /obj
set FR [new FogRemover]
set_pos $FR {280 14 15}
call_method $FR fog_remove 0 46 22
adaptive_sound marker lavawelt {280 18 15} 1000
adaptive_sound changethemenow lavawelt


show_loading 0

#set_brightness 2

log "CampaignLava.tcl loaded..."


sm_force_zone Lava
