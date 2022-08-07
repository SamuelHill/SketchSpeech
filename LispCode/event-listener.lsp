
(defclass event-printer (cog:cogsketch-module)
  ((out-stream
    :documentation "Stream to which the event information should be printed."
    :accessor out-stream :initarg :out-stream :initform *standard-output*))
  (:documentation "This CogSketch module listens to various events and
                   prints information about them."))

(defun init-event-printer (&optional (out-stream *standard-output*))
  (let ((printer (make-instance 'event-printer
                   :out-stream out-stream)))
    (cog:subscribe-to-events printer :all
      :new-glyphs
      :new-free-ink
      :items-deleted)
    printer))

(defmethod cog:new-glyphs ((printer event-printer)
                           sender sketch glyphs)
  (format (out-stream printer)
      "~2%~A added new glyphs to sketch ~A:"
    sender (cog:user-namestring sketch))
  (dolist (glyph glyphs)
    (format (out-stream printer)
        "~%* ~A" (cog:name glyph))
    (cl-user::insert (cog:name glyph) :receiver interaction-manager))
  glyphs)
