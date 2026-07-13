show_loading yes
obj_clear

call data/GameScripts/startseqs.tcl

call_method [obj_query this -class Trigger_Auftrag_Odin -limit 1] set_context "tutorial"

show_loading no

