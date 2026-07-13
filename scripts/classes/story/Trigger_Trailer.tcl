// Sonder-Trigger für Trailer u.ä.

def_class Trigger_Kuessen none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
		set sequencescript "Kuessen"
		trigger create this any_object "sequencer_activate"
		trigger set_target_class this "Zwerg"
	}
}


def_class Trigger_Kamera none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
		set sequencescript "Kamera"
		trigger create this single_timer "sequencer_activate"
		trigger set_timer this 2
	}
}


def_class Trigger_Trailer_Zwergenauflauf none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
		set sequencescript "Trailer_Zwergenauflauf"
		trigger create this single_timer "sequencer_activate"
		trigger set_timer this 5
	}
}


def_class Trigger_Trailer_Troll none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
		set sequencescript "Trailer_Troll"
		trigger create this single_timer "sequencer_activate"
		trigger set_timer this 2
	}
}


def_class Trigger_Trailer_Labor none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
		set sequencescript "Trailer_Labor"
		trigger create this any_object "sequencer_activate"
		trigger set_target_range this 2.0
		trigger set_target_type this gnome
		trigger set_target_owner this 3
		trigger set_target_count this 1
	}
}


def_class Trigger_Trailer_Wuker none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
		set sequencescript "Trailer_Wuker"
		trigger create this single_timer "sequencer_activate"
		trigger set_timer this 2
	}
}


def_class Trigger_Maschine_Kaputt none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
		set sequencescript "Trailer_Maschine_Kaputt"
		trigger create this single_timer "sequencer_activate"
		trigger set_timer this 2
	}
}



def_class Trigger_G none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
		set sequencescript "Kamera"
		trigger create this any_object "sequencer_activate"
		trigger set_target_range this 50.0
		trigger set_target_type this gnome
		trigger set_target_owner this 1
		trigger set_target_count this 1
	}
}


def_class Trigger_Regentanz none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
		set sequencescript "Regentanz"
		trigger create this single_timer "sequencer_activate"
		trigger set_timer this 5
	}
}


def_class Trigger_Comeback none dummy 0 {} {
	call scripts/classes/story/sequencer.tcl
	def_event evt_timer0
	obj_init {
		set_selectable this 0
		set_hoverable this 0
		timer_event this evt_timer0 -repeat 0 -userid 0 -attime [expr [gettime]+1]
		call scripts/classes/story/sequencer.tcl
	}

	handle_event evt_timer0 {
		set sequencescript "BackStreetBoys"
		trigger create this any_object "sequencer_activate"
		trigger set_target_range this 5
		trigger set_target_class this "Zwerg"

	}
}



