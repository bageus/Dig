// ######################### Campaign load ##########################
map create 512 640
sm_draw_stone -border
//sm_draw_stone -funnel

generate_color_variation 0 0 512 640 0
#set_fow_begin 133
set_light_begin 33
set_view_begin 23
set_view 246.7 28.8 1.478 -0.29 0.05		;# set inital camera view (x y zoom)

set temppos {208 0};call templates/tcl/urw_unq_start.tcl

//adaptive_sound marker start {294 30 10}
//adaptive_sound marker cave {294 45 10}

sel /obj
set FR [new FogRemover]
set_undeletable $FR 1
set_pos $FR {239 14 15}
call_method $FR fog_remove 0 46 22
adaptive_sound marker cave {280 14 15} 1000
adaptive_sound changethemenow cave

set fs [new Feuerstelle]
set_owner $fs 0
set_boxed $fs 1
set_pos $fs {246.8 30.5 9.8}

set tl [lnand 0 [obj_query $fs -class {Trigger_Urw_006 Trigger_Gleipnir}]]
foreach item $tl {
	set_undeletable $item 0
	del $item
}

show_loading 0

log "SandboxUrw.tcl loaded..."

