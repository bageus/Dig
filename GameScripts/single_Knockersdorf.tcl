show_loading yes
obj_clear
map create 300 300

;# create dummy object !!! don't touch it !!!
sel /obj


set_view 28.2 50.1 1.0 -0.3 0.0		;# set inital camera view (x y zoom)
set_fow_begin 200

set_pos [qnew Zwerg] {28.25 50.4 8.0}

call templates/swf_unq_lore_001.tcl
MapTemplateSet 156 32

call templates/swf_unq_lore_003.tcl
MapTemplateSet 156 60

call templates/swf_unq_lore_002.tcl
MapTemplateSet 188 88

call templates/swf_unq_lore_004.tcl
MapTemplateSet 156 108

call templates/swf_unq_knk_001_a.tcl
MapTemplateSet 112 76

call templates/swf_unq_knk_002_a.tcl
MapTemplateSet 76 60

call templates/swf_unq_knk_003_a.tcl
MapTemplateSet 100 36

call templates/swf_unq_knk_004_a.tcl
MapTemplateSet 112 56

call templates/swf_unq_knk_004_b.tcl
MapTemplateSet 64 36

call templates/swf_gng_002_a.tcl
MapTemplateSet 100 56

call templates/swf_gng_002_b.tcl
MapTemplateSet 56 48

call templates/swf_gng_002_c.tcl
MapTemplateSet 84 20

call templates/swf_gng_003_e.tcl
MapTemplateSet 116 24

call templates/swf_gng_003_f.tcl
MapTemplateSet 104 24

call templates/swf_gng_004_a.tcl
MapTemplateSet 80 56

call templates/swf_gng_004_b.tcl
MapTemplateSet 128 120

call templates/swf_gng_004_c.tcl
MapTemplateSet 60 28

call templates/swf_gng_004_d.tcl
MapTemplateSet 76 28

call templates/swf_gng_007_a.tcl
MapTemplateSet 96 52

call templates/swf_gng_008_a.tcl
MapTemplateSet 108 60

call templates/swf_gng_009_a.tcl
MapTemplateSet 108 76

call templates/swf_gng_009_b.tcl
MapTemplateSet 52 36

call templates/swf_gng_013_a.tcl
MapTemplateSet 96 48

call templates/swf_gng_014_a.tcl
MapTemplateSet 108 56

call templates/swf_gng_014_b.tcl
MapTemplateSet 144 68

call templates/swf_gng_015_a.tcl
MapTemplateSet 108 68

call templates/swf_gng_017_a.tcl
MapTemplateSet 108 72

call templates/swf_gng_018_a.tcl
MapTemplateSet 108 88

call templates/swf_gng_018_b.tcl
MapTemplateSet 52 48

call templates/swf_gng_019_a.tcl
MapTemplateSet 96 56

call templates/swf_gng_020_a.tcl
MapTemplateSet 152 96

call templates/swf_gng_021_a.tcl
MapTemplateSet 76 56

call templates/swf_gng_021_b.tcl
MapTemplateSet 72 72

call templates/swf_gng_021_c.tcl
MapTemplateSet 124 120

call templates/swf_gng_021_d.tcl
MapTemplateSet 80 20

call templates/swf_gng_021_h.tcl
MapTemplateSet 44 24

call templates/swf_gng_024_a.tcl
MapTemplateSet 148 8

call templates/swf_gng_026_a.tcl
MapTemplateSet 128 16

call templates/swf_gng_027_a.tcl
MapTemplateSet 140 104

call templates/swf_gng_027_b.tcl
MapTemplateSet 144 72

call templates/swf_gng_032_a.tcl
MapTemplateSet 144 84

call templates/swf_gng_034_a.tcl
MapTemplateSet 144 116

call templates/swf_gng_035_a.tcl
MapTemplateSet 48 24

call templates/swf_gng_038_a.tcl
MapTemplateSet 92 20

call templates/swf_gng_042_a.tcl
MapTemplateSet 140 12

call templates/swf_gng_045_a.tcl
MapTemplateSet 140 96

show_loading no

//#stopif NOT
//#stopifnot NOT
sm_def_temp_group temp_name {
				156 32 swf_unq_lore_001.tcl
				156 60 swf_unq_lore_003.tcl
				188 88 swf_unq_lore_002.tcl
				156 108 swf_unq_lore_004.tcl
				112 76 swf_unq_knk_001_a.tcl
				76 60 swf_unq_knk_002_a.tcl
				100 36 swf_unq_knk_003_a.tcl
				112 56 swf_unq_knk_004_a.tcl
				64 36 swf_unq_knk_004_b.tcl
				100 56 swf_gng_002_a.tcl
				56 48 swf_gng_002_b.tcl
				84 20 swf_gng_002_c.tcl
				116 24 swf_gng_003_e.tcl
				104 24 swf_gng_003_f.tcl
				80 56 swf_gng_004_a.tcl
				128 120 swf_gng_004_b.tcl
				60 28 swf_gng_004_c.tcl
				76 28 swf_gng_004_d.tcl
				96 52 swf_gng_007_a.tcl
				108 60 swf_gng_008_a.tcl
				108 76 swf_gng_009_a.tcl
				52 36 swf_gng_009_b.tcl
				96 48 swf_gng_013_a.tcl
				108 56 swf_gng_014_a.tcl
				144 68 swf_gng_014_b.tcl
				108 68 swf_gng_015_a.tcl
				108 72 swf_gng_017_a.tcl
				108 88 swf_gng_018_a.tcl
				52 48 swf_gng_018_b.tcl
				96 56 swf_gng_019_a.tcl
				152 96 swf_gng_020_a.tcl
				76 56 swf_gng_021_a.tcl
				72 72 swf_gng_021_b.tcl
				124 120 swf_gng_021_c.tcl
				80 20 swf_gng_021_d.tcl
				44 24 swf_gng_021_h.tcl
				148 8 swf_gng_024_a.tcl
				128 16 swf_gng_026_a.tcl
				140 104 swf_gng_027_a.tcl
				144 72 swf_gng_027_b.tcl
				144 84 swf_gng_032_a.tcl
				144 116 swf_gng_034_a.tcl
				48 24 swf_gng_035_a.tcl
				92 20 swf_gng_038_a.tcl
				140 12 swf_gng_042_a.tcl
				140 96 swf_gng_045_a.tcl
				}
