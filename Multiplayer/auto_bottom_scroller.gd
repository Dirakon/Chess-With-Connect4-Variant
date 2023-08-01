extends TextEdit

func _on_text_changed():
	var line_count = get_line_count()
	if line_count != 0:
		set_line_as_last_visible(line_count - 1)
